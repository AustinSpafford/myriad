using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

[RequireComponent(typeof(ParticleSpatializer))]
[RequireComponent(typeof(AudioShaderUniformCollector))]
[RequireComponent(typeof(SwarmForcefieldCollector))]
public class SwarmSimulator : MonoBehaviour
{
	public int SwarmerCount = 1000;
	public int MaxForcefieldCount = 16;

	public float SwarmerNeighborhoodRadius = 0.25f;
	public int MaxNeighborCount = 100;
	
	public float SwarmerSpeedIdle = 0.5f;
	
	public float IdealVelocityBlendingRate = 10.0f;

	public float NeighborAttractionScalar = 0.1f;
	public float NeighborCollisionAvoidanceScalar = 10.0f;
	public float NeighborAlignmentScalar = 0.1f;

	public float SwarmerModelScale = 0.04f;

	public float LocalTimeScale = 1.0f;

	public ComputeShader BehaviorComputeShader;
	public ComputeShader CommonSwarmComputeShader;

	public bool DebugEnabled = false;

	public void Awake()
	{
		audioShaderUniformCollector = GetComponent<AudioShaderUniformCollector>();
		forcefieldCollector = GetComponent<SwarmForcefieldCollector>();
		particleSpatializer = GetComponent<ParticleSpatializer>();
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

			audioShaderUniformCollector.CollectComputeShaderUniforms(BehaviorComputeShader);

			AdvanceSwarmers(localDeltaTime, swarmerNeighborhoods);

			lastRenderedFrameIndex = frameIndex;
			lastRenderedDateTime = currentTime;
		}

		return swarmerStateBuffers.CurrentComputeBuffer;
	}

	private const int ForcefieldsComputeBufferCount = (2 * 2); // Double-buffered for each eye, to help avoid having SetData() cause a pipeline-stall if the data's still being read by the GPU.
	
	private AudioShaderUniformCollector audioShaderUniformCollector = null;
	private SwarmForcefieldCollector forcefieldCollector = null;
	private ParticleSpatializer particleSpatializer = null;

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
			BehaviorComputeShader.SetInt("u_total_voxel_count", particleSpatializer.TotalVoxelCount);			

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
			
			BehaviorComputeShader.SetFloat("u_swarmer_speed_idle", SwarmerSpeedIdle);
			
			BehaviorComputeShader.SetFloat("u_ideal_velocity_blending_rate", IdealVelocityBlendingRate);

			BehaviorComputeShader.SetFloat("u_neighbor_attraction_scalar", NeighborAttractionScalar);
			BehaviorComputeShader.SetFloat("u_neighbor_collision_avoidance_scalar", NeighborCollisionAvoidanceScalar);
			BehaviorComputeShader.SetFloat("u_neighbor_alignment_scalar", NeighborAlignmentScalar);
			
			BehaviorComputeShader.SetFloat("u_swarmer_model_scale", SwarmerModelScale);
							
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

	private void SetSwarmerTransformForRandomSetup(
		ref SwarmShaderSwarmerState inoutSwarmerState)
	{
		inoutSwarmerState.Position = 
			Vector3.Scale(new Vector3(3.0f, 0.5f, 3.0f), UnityEngine.Random.insideUnitSphere);

		inoutSwarmerState.Velocity = 
			(0.05f * UnityEngine.Random.onUnitSphere); // Just a gentle nudge to indicate a direction.

		inoutSwarmerState.LocalUp = UnityEngine.Random.onUnitSphere;
	}

	private void SetSwarmerTransformForTripletTiledFloor(
		int swarmerIndex,
		ref SwarmShaderSwarmerState inoutSwarmerState)
	{
		// NOTE: For simplicity the tiles are pairs of triplets (forming a parallelogram).
		int tileIndex = (swarmerIndex / 6);
		int patternIndex = (swarmerIndex % 6);
		
		int tilingStride = Mathf.CeilToInt(Mathf.Sqrt(SwarmerCount / 6));
		int tileRowIndex = ((tileIndex / tilingStride) - (tilingStride / 2));
		int tileColumnIndex = ((tileIndex % tilingStride) - (tilingStride / 2));

		Vector3 swarmerCenterToRightWingtip = (
			SwarmerModelScale *
			new Vector3(
				(2.0f * Mathf.Cos(30.0f * Mathf.Deg2Rad)),
				0.0f,
				(-1.0f * Mathf.Sin(30.0f * Mathf.Deg2Rad))));

		Matrix4x4 patternTransform = Matrix4x4.identity;

		if (patternIndex < 3)
		{
			patternTransform = (
				Matrix4x4.TRS(
					(-1.0f * swarmerCenterToRightWingtip), 
					Quaternion.identity,
					Vector3.one) * 
				patternTransform);
			
			patternTransform = (
				Matrix4x4.TRS(
					Vector3.zero, 
					Quaternion.AngleAxis((90.0f + (120.0f * patternIndex)), Vector3.up),
					Vector3.one) * 
				patternTransform);
		}
		else
		{
			Vector3 swarmerCenterToLeftWingtip = swarmerCenterToRightWingtip;
			swarmerCenterToLeftWingtip.x *= -1.0f;

			patternTransform = (
				Matrix4x4.TRS(
					(-1.0f * swarmerCenterToLeftWingtip), 
					Quaternion.identity,
					Vector3.one) * 
				patternTransform);
			
			patternTransform = (
				Matrix4x4.TRS(
					Vector3.zero, 
					Quaternion.AngleAxis((-90.0f + (120.0f * patternIndex)), Vector3.up),
					Vector3.one) * 
				patternTransform);
		}

		Vector3 patternPosition = patternTransform.MultiplyPoint(Vector3.zero);
		Vector3 patternVelocity = (0.05f * patternTransform.MultiplyVector(Vector3.forward)); // Just a gentle nudge to indicate a direction
		
		Vector3 tileSize = new Vector3(
			(6.0f * (swarmerCenterToRightWingtip.x * Mathf.Sin(60.0f * Mathf.Deg2Rad))),
			0.0f,
			(3.0f * swarmerCenterToRightWingtip.x));

		Vector3 tilePosition = 
			Vector3.Scale(
				tileSize,
				new Vector3(
					(tileColumnIndex / 2.0f), 
					0.0f, 
					tileRowIndex + (0.5f * (tileColumnIndex % 2))));
		
		inoutSwarmerState.Position = (tilePosition + patternPosition);
		inoutSwarmerState.Velocity = patternVelocity;
		inoutSwarmerState.LocalUp = patternTransform.MultiplyVector(Vector3.up);
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

				while (forcefieldsBufferQueue.Count < ForcefieldsComputeBufferCount)
				{
					forcefieldsBufferQueue.Enqueue(
						new TypedComputeBuffer<SwarmShaderForcefieldState>(MaxForcefieldCount));
				}

				// NOTE: There's no need to immediately initialize the buffers, since they will be populated per-frame.
			}

			if (swarmerStateBuffers.IsInitialized == false)
			{
				var initialSwarmers = new List<SwarmShaderSwarmerState>(SwarmerCount);
				
				for (int swarmerIndex = 0; swarmerIndex < SwarmerCount; ++swarmerIndex)
				{
					var newSwarmerState = new SwarmShaderSwarmerState();

					//SetSwarmerTransformForRandomSetup(ref newSwarmerState);
					SetSwarmerTransformForTripletTiledFloor(swarmerIndex, ref newSwarmerState);

					initialSwarmers.Add(newSwarmerState);
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
