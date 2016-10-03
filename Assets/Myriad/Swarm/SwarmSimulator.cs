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
	public int MaxNeighborCount = 100;

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
		particleSpatializer.SetDesiredMaxParticleCount(SwarmerCount);
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
			(lastRenderedFrameIndex != frameIndex) &&
			particleSpatializer.IsInitialized)
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

			ParticleSpatializer.NeighborhoodResults swarmerNeighborhoods = BuildSwarmerNeighborhoods();

			AdvanceSwarmers(localDeltaTime, swarmerNeighborhoods);

			lastRenderedFrameIndex = frameIndex;
			lastRenderedDateTime = currentTime;
		}

		return swarmerStateBuffers.CurrentComputeBuffer;
	}

	private const int ForcefieldsComputeBufferCount = (2 * 2); // Double-buffered for each eye, to help avoid having SetData() cause a pipeline-stall if the data's still being read by the GPU.
	
	private ParticleSpatializer particleSpatializer = null;
	private SwarmForcefieldCollector forcefieldCollector = null;

	private Queue<TypedComputeBuffer<SwarmShaderForcefieldState> > forcefieldsBufferQueue = null;
	private PingPongComputeBuffers<SwarmShaderSwarmerState> swarmerStateBuffers = new PingPongComputeBuffers<SwarmShaderSwarmerState>();

	private int advanceSwarmersKernel = -1;
	private int kernelForExtractSwarmerPositions = -1;

	private List<SwarmShaderForcefieldState> scratchForcefieldStateList = new List<SwarmShaderForcefieldState>();

	private int lastRenderedFrameIndex = -1;
	private DateTime lastRenderedDateTime = DateTime.UtcNow;

	private ParticleSpatializer.NeighborhoodResults BuildSwarmerNeighborhoods()
	{
		TypedComputeBuffer<SpatializerShaderParticlePosition> scratchParticlePositionsBuffer =
			particleSpatializer.GetScratchParticlePositionsComputeBuffer(SwarmerCount);

		// Extract the swarmer positions so the spatializer can stay generic.
		{
			CommonSwarmComputeShader.SetInt("u_swarmer_count", SwarmerCount);

			CommonSwarmComputeShader.SetBuffer(
				kernelForExtractSwarmerPositions, 
				"u_readable_swarmers", 
				swarmerStateBuffers.CurrentComputeBuffer);

			CommonSwarmComputeShader.SetBuffer(
				kernelForExtractSwarmerPositions, 
				"u_out_swarmer_positions", 
				scratchParticlePositionsBuffer);
		
			ComputeShaderHelpers.DispatchLinearComputeShader(
				CommonSwarmComputeShader, 
				kernelForExtractSwarmerPositions, 
				SwarmerCount);
		}

		ParticleSpatializer.NeighborhoodResults result = 
			particleSpatializer.BuildNeighborhoodLookupBuffers(
				SwarmerCount,
				scratchParticlePositionsBuffer,
				SwarmerNeighborhoodRadius);

		return result;
	}

	private void AdvanceSwarmers(
		float localDeltaTime,
		ParticleSpatializer.NeighborhoodResults swarmerNeighborhoods)
	{
		// Bind the swarmers.
		{
			BehaviorComputeShader.SetInt("u_swarmer_count", SwarmerCount);

			swarmerStateBuffers.SwapBuffersAndBindToShaderKernel(
				BehaviorComputeShader,
				advanceSwarmersKernel,
				"u_readable_swarmers",
				"u_out_next_swarmers");
		}

		// Bind the spatialization results.
		{
			BehaviorComputeShader.SetInt("u_voxel_count_per_axis", particleSpatializer.VoxelsPerAxis);

			BehaviorComputeShader.SetBuffer(advanceSwarmersKernel, "u_spatialization_voxel_particle_pairs", swarmerNeighborhoods.VoxelParticlePairsBuffer);
			BehaviorComputeShader.SetBuffer(advanceSwarmersKernel, "u_spatialization_voxels", swarmerNeighborhoods.SpatializationVoxelsBuffer);
			BehaviorComputeShader.SetBuffer(advanceSwarmersKernel, "u_spatialization_neighborhoods", swarmerNeighborhoods.NeighborhoodsBuffer);
		}

		// Bind the forcefields.
		{
			TypedComputeBuffer<SwarmShaderForcefieldState> forcefieldsComputeBuffer;
			int activeForcefieldCount;
			BuildForcefieldsBuffer(
				out forcefieldsComputeBuffer,
				out activeForcefieldCount);

			BehaviorComputeShader.SetInt("u_forcefield_count", activeForcefieldCount);

			BehaviorComputeShader.SetBuffer(advanceSwarmersKernel, "u_forcefields", forcefieldsComputeBuffer);
		}

		// Bind behavior/advancement constants.
		{
			BehaviorComputeShader.SetFloat("u_neighborhood_radius", SwarmerNeighborhoodRadius);
			BehaviorComputeShader.SetInt("u_max_neighbor_count", MaxNeighborCount);			
				
			BehaviorComputeShader.SetFloat("u_delta_time", localDeltaTime);
		}

		ComputeShaderHelpers.DispatchLinearComputeShader(
			BehaviorComputeShader, 
			advanceSwarmersKernel, 
			swarmerStateBuffers.ElementCount);
	}

	private void BuildForcefieldsBuffer(
		out TypedComputeBuffer<SwarmShaderForcefieldState> outPooledForcefieldsBuffer,
		out int outActiveForcefieldCount)
	{
		// Grab the oldest buffer off the queue, and move it back to mark it as the most recently touched buffer.
		TypedComputeBuffer<SwarmShaderForcefieldState> targetForcefieldsBuffer = forcefieldsBufferQueue.Dequeue();
		forcefieldsBufferQueue.Enqueue(targetForcefieldsBuffer);	

		forcefieldCollector.CollectForcefields(
			transform.localToWorldMatrix,
			ref scratchForcefieldStateList);

		if (scratchForcefieldStateList.Count > targetForcefieldsBuffer.count)
		{
			Debug.LogWarningFormat(
				"Discarding some forcefields since [{0}] were wanted, but only [{1}] can be passed to the shader.",
				scratchForcefieldStateList.Count,
				targetForcefieldsBuffer.count);

			scratchForcefieldStateList.RemoveRange(
				targetForcefieldsBuffer.count, 
				(scratchForcefieldStateList.Count - targetForcefieldsBuffer.count));
		}	

		targetForcefieldsBuffer.SetData(scratchForcefieldStateList.ToArray());

		outPooledForcefieldsBuffer = targetForcefieldsBuffer;
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

			if (forcefieldsBufferQueue == null)
			{
				forcefieldsBufferQueue = 
					new Queue<TypedComputeBuffer<SwarmShaderForcefieldState> >(ForcefieldsComputeBufferCount);

				for (int index = 0; index < ForcefieldsComputeBufferCount; ++index)
				{
					forcefieldsBufferQueue.Enqueue(
						new TypedComputeBuffer<SwarmShaderForcefieldState>(MaxForcefieldCount));
				}

				// NOTE: There's no need to immediately initialize the buffers, since they will be populated per-frame.
			}

			if (swarmerStateBuffers.IsInitialized == false)
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
				
				swarmerStateBuffers.TryAllocateComputeBuffersWithValues(initialSwarmers.ToArray());
			}
			
			if ((advanceSwarmersKernel != -1) &&
				(kernelForExtractSwarmerPositions != -1) &&
				(forcefieldsBufferQueue != null) &&
				swarmerStateBuffers.IsInitialized)
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
		if (forcefieldsBufferQueue != null)
		{
			foreach (var forcefieldsBuffer in forcefieldsBufferQueue)
			{
				forcefieldsBuffer.Release();
			}

			forcefieldsBufferQueue = null;
		}

		swarmerStateBuffers.ReleaseBuffers();

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffers released.");
		}
	}
}
