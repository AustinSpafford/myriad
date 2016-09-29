using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

[RequireComponent(typeof(SwarmForcefieldCollector))]
[RequireComponent(typeof(ParticleSpatializer))]
public class SwarmSimulator : MonoBehaviour
{
	public int SwarmerCount = 1000;
	public int MaxForcefieldCount = 16;

	public float SwarmerNeighborhoodRadius = 0.25f;
	public int MaximumNeighborsPerSwarmer = 100;

	public float LocalTimeScale = 1.0f;

	public ComputeShader BehaviorComputeShader;
	public ComputeShader CommonSwarmComputeShader;

	public bool DebugEnabled = false;

	public void Awake()
	{
		particleSpatializer = GetComponent<ParticleSpatializer>();
		forcefieldCollector = GetComponent<SwarmForcefieldCollector>();
	}

	public void Start()
	{
		particleSpatializer.SetMaxParticleCount(SwarmerCount);
	}

	public void OnEnable()
	{
		TryAllocateBuffers();
	}

	public void OnDisable()
	{
		ReleaseBuffers();
	}

	public TypedComputeBuffer<SwarmShaderSwarmerState> TryBuildSwarmersForRenderFrameIndex(
		int frameIndex)
	{
		// If the swarm needs to be advanced to the requested frame.
		if ((BehaviorComputeShader != null) &&
			(lastRenderedFrameIndex != frameIndex))
		{
			DateTime currentTime = DateTime.UtcNow;
			
			bool applicationIsPaused = 
#if UNITY_EDITOR
				EditorApplication.isPaused;
#else
				false;
#endif

			// The editor doesn't alter the timescale for us when the sim is paused, so we need to do it ourselves.
			float timeScale = (
				LocalTimeScale * 
				(applicationIsPaused ? 0.0f : Time.timeScale));

			// Step ourselves based on the *graphics* framerate (since we're part of the rendering pipeline),
			// but make sure to avoid giant steps whenever rendering is paused.
			float localDeltaTime = 
				Mathf.Min(
					(float)(timeScale * (currentTime - lastRenderedDateTime).TotalSeconds),
					Time.maximumDeltaTime);

			SpatializeSwarmers();

			AdvanceSwarmers(localDeltaTime);

			lastRenderedFrameIndex = frameIndex;
			lastRenderedDateTime = currentTime;
		}

		return swarmerStateComputeBuffers.CurrentComputeBuffer;
	}

	private const int ForcefieldsComputeBufferCount = (2 * 2); // Double-buffered for each eye, to help avoid having SetData() cause a pipeline-stall if the data's still being read by the GPU.
	
	private ParticleSpatializer particleSpatializer = null;
	private SwarmForcefieldCollector forcefieldCollector = null;

	private Queue<TypedComputeBuffer<SwarmShaderForcefieldState> > forcefieldsComputeBufferQueue = null;
	private PingPongComputeBuffers<SwarmShaderSwarmerState> swarmerStateComputeBuffers = new PingPongComputeBuffers<SwarmShaderSwarmerState>();
	
	private TypedComputeBuffer<SpatializerShaderNeighborhood> spatializationNeighborhoodsComputeBuffer = null;
	private TypedComputeBuffer<SpatializerShaderParticleIndex> spatializationSwarmerIndexComputeBuffer = null;
	private TypedComputeBuffer<SpatializerShaderParticlePosition> spatializationSwarmerPositionComputeBuffer = null;

	private int advanceSwarmersKernel = -1;
	private int kernelForExtractSwarmerPositions = -1;

	private List<SwarmShaderForcefieldState> scratchForcefieldStateList = new List<SwarmShaderForcefieldState>();

	private int lastRenderedFrameIndex = -1;
	private DateTime lastRenderedDateTime = DateTime.UtcNow;

	private void SpatializeSwarmers()
	{
		// Extract the swarmer positions so the spatializer can stay generic.
		{
			CommonSwarmComputeShader.SetInt("u_swarmer_count", SwarmerCount);

			CommonSwarmComputeShader.SetBuffer(
				kernelForExtractSwarmerPositions, 
				"u_readable_swarmers", 
				swarmerStateComputeBuffers.CurrentComputeBuffer);

			CommonSwarmComputeShader.SetBuffer(
				kernelForExtractSwarmerPositions, 
				"u_out_swarmer_positions", 
				spatializationSwarmerPositionComputeBuffer);
		
			ComputeShaderHelpers.DispatchLinearComputeShader(
				CommonSwarmComputeShader, 
				kernelForExtractSwarmerPositions, 
				SwarmerCount);
		}

		particleSpatializer.BuildNeighborhoodLookupBuffers(
			SwarmerCount,
			spatializationSwarmerPositionComputeBuffer,
			SwarmerNeighborhoodRadius,
			MaximumNeighborsPerSwarmer,
			spatializationSwarmerIndexComputeBuffer,
			spatializationNeighborhoodsComputeBuffer);
	}

	private void AdvanceSwarmers(
		float localDeltaTime)
	{
		TypedComputeBuffer<SwarmShaderForcefieldState> forcefieldsComputeBuffer;
		int activeForcefieldCount;
		BuildForcefieldsBuffer(
			out forcefieldsComputeBuffer,
			out activeForcefieldCount);
		
		BehaviorComputeShader.SetInt("u_forcefield_count", activeForcefieldCount);

		BehaviorComputeShader.SetBuffer(advanceSwarmersKernel, "u_forcefields", forcefieldsComputeBuffer);
		
		BehaviorComputeShader.SetInt("u_swarmer_count", SwarmerCount);

		swarmerStateComputeBuffers.SwapBuffersAndBindToShaderKernel(
			BehaviorComputeShader,
			advanceSwarmersKernel,
			"u_readable_swarmers",
			"u_out_next_swarmers");

		BehaviorComputeShader.SetFloat("u_neighborhood_radius", SwarmerNeighborhoodRadius);
				
		BehaviorComputeShader.SetFloat("u_delta_time", localDeltaTime);

		ComputeShaderHelpers.DispatchLinearComputeShader(
			BehaviorComputeShader, 
			advanceSwarmersKernel, 
			swarmerStateComputeBuffers.ElementCount);
	}

	private void BuildForcefieldsBuffer(
		out TypedComputeBuffer<SwarmShaderForcefieldState> outPooledForcefieldsComputeBuffer,
		out int outActiveForcefieldCount)
	{
		// Grab the oldest buffer off the queue, and move it back to mark it as the most recently touched buffer.
		TypedComputeBuffer<SwarmShaderForcefieldState> targetComputeBuffer = forcefieldsComputeBufferQueue.Dequeue();
		forcefieldsComputeBufferQueue.Enqueue(targetComputeBuffer);	

		forcefieldCollector.CollectForcefields(
			transform.localToWorldMatrix,
			ref scratchForcefieldStateList);

		if (scratchForcefieldStateList.Count > targetComputeBuffer.count)
		{
			Debug.LogWarningFormat(
				"Discarding some forcefields since [{0}] were wanted, but only [{1}] can be passed to the shader.",
				scratchForcefieldStateList.Count,
				targetComputeBuffer.count);

			scratchForcefieldStateList.RemoveRange(
				targetComputeBuffer.count, 
				(scratchForcefieldStateList.Count - targetComputeBuffer.count));
		}	

		targetComputeBuffer.SetData(scratchForcefieldStateList.ToArray());

		outPooledForcefieldsComputeBuffer = targetComputeBuffer;
		outActiveForcefieldCount = scratchForcefieldStateList.Count;
	}

	private bool TryAllocateBuffers()
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else if ((BehaviorComputeShader != null) &&
			(CommonSwarmComputeShader != null))
		{
			advanceSwarmersKernel = 
				BehaviorComputeShader.FindKernel("kernel_advance_swarmer_states");

			kernelForExtractSwarmerPositions = 
				CommonSwarmComputeShader.FindKernel("kernel_extract_swarmer_positions");

			if (forcefieldsComputeBufferQueue == null)
			{
				forcefieldsComputeBufferQueue = 
					new Queue<TypedComputeBuffer<SwarmShaderForcefieldState> >(ForcefieldsComputeBufferCount);

				for (int index = 0; index < ForcefieldsComputeBufferCount; ++index)
				{
					forcefieldsComputeBufferQueue.Enqueue(
						new TypedComputeBuffer<SwarmShaderForcefieldState>(MaxForcefieldCount));
				}

				// NOTE: There's no need to immediately initialize the buffers, since they will be populated per-frame.
			}

			if (swarmerStateComputeBuffers.IsInitialized == false)
			{
				var initialSwarmers = new List<SwarmShaderSwarmerState>(SwarmerCount);
				
				for (int index = 0; index < SwarmerCount; ++index)
				{
					initialSwarmers.Add(new SwarmShaderSwarmerState()
					{
						Position = (0.5f * Vector3.Scale(UnityEngine.Random.onUnitSphere, transform.localScale)),
						Velocity = (0.05f * UnityEngine.Random.onUnitSphere), // Just a gentle nudge to indicate a direction.
						LocalUp = UnityEngine.Random.onUnitSphere,
					});
				}
				
				swarmerStateComputeBuffers.TryAllocateComputeBuffersWithValues(initialSwarmers.ToArray());
			}

			if (spatializationNeighborhoodsComputeBuffer == null)
			{
				spatializationNeighborhoodsComputeBuffer = 
					new TypedComputeBuffer<SpatializerShaderNeighborhood>(SwarmerCount);
			}
			
			if (spatializationSwarmerIndexComputeBuffer == null)
			{
				spatializationSwarmerIndexComputeBuffer = 
					new TypedComputeBuffer<SpatializerShaderParticleIndex>(SwarmerCount);
			}

			if (spatializationSwarmerPositionComputeBuffer == null)
			{
				spatializationSwarmerPositionComputeBuffer = 
					new TypedComputeBuffer<SpatializerShaderParticlePosition>(SwarmerCount);
			}
			
			if ((advanceSwarmersKernel != -1) &&
				(kernelForExtractSwarmerPositions != -1) &&
				(forcefieldsComputeBufferQueue != null) &&
				swarmerStateComputeBuffers.IsInitialized &&
				(spatializationNeighborhoodsComputeBuffer != null) &&
				(spatializationSwarmerIndexComputeBuffer != null) &&
				(spatializationSwarmerPositionComputeBuffer != null))
			{
				result = true;
			}
			else
			{
				// Abort any partial-allocations.
				ReleaseBuffers();
			}
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer allocation attempted. [Success={0}]", result);
		}

		return result;
	}

	private void ReleaseBuffers()
	{
		// Release all of the forcefield compute buffers.
		if (forcefieldsComputeBufferQueue != null)
		{
			foreach (var forcefieldsComputeBuffer in forcefieldsComputeBufferQueue)
			{
				forcefieldsComputeBuffer.Release();
			}

			forcefieldsComputeBufferQueue = null;
		}

		swarmerStateComputeBuffers.ReleaseBuffers();

		if (spatializationNeighborhoodsComputeBuffer != null)
		{
			spatializationNeighborhoodsComputeBuffer.Release();
			spatializationNeighborhoodsComputeBuffer = null;
		}

		if (spatializationSwarmerIndexComputeBuffer != null)
		{
			spatializationSwarmerIndexComputeBuffer.Release();
			spatializationSwarmerIndexComputeBuffer = null;
		}

		if (spatializationSwarmerPositionComputeBuffer != null)
		{
			spatializationSwarmerPositionComputeBuffer.Release();
			spatializationSwarmerPositionComputeBuffer = null;
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffers released.");
		}
	}
}
