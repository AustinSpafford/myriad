using UnityEngine;
using System;
using System.Collections;

public class ParticleSpatializer : MonoBehaviour
{
	[Tooltip("Note that the memory/time costs are proportional to this number _cubed_!")]
	public int VoxelsPerAxis = 64;

	public ComputeShader SpatializerComputeShader;

	public bool DebugCaptureSingleFrame = false;
	public bool DebugMessagesEnabled = false;
	
	public int MaxParticleCount { get; private set; }

	public void OnEnable()
	{
		TryAllocateBuffers();
	}

	public void OnDisable()
	{
		ReleaseBuffers();
	}

	public void SetMaxParticleCount(
		int maxParticleCount)
	{
		if (MaxParticleCount != maxParticleCount)
		{
			MaxParticleCount = maxParticleCount;

			if (isActiveAndEnabled)
			{
				ReleaseBuffers();
				TryAllocateBuffers();
			}
		}
	}

	public void BuildNeighborhoodLookupBuffers(
		int particleCount,
		TypedComputeBuffer<SpatializerShaderParticlePosition> particlePositionsBuffer,
		float neighborhoodRadius,
		int maximumNeighborsPerParticle,
		TypedComputeBuffer<SpatializerShaderParticleIndex> outParticleIndicesBuffer,
		TypedComputeBuffer<SpatializerShaderNeighborhood> outPerParticleNeighborhoodsBuffer)
	{
		float voxelSize = (2.0f * neighborhoodRadius);

		if (particleCount > MaxParticleCount)
		{
			throw new InvalidOperationException(string.Format(
				"Only [{0}] particles are allocated for, but the request is for [{1}] particles. Call SetMaxParticleCount().",
				MaxParticleCount,
				particleCount));
		}

#pragma warning disable 0219 // Warning that variable is assigned but never referenced (these variables are for inspection in the debugger).
		SpatializerShaderParticlePosition[] debugParticlePositions = null;
		SpatializerShaderVoxelParticlePair[] debugUnsortedParticlePairs = null;
#pragma warning restore 0219

		if (DebugCaptureSingleFrame)
		{
			debugParticlePositions = particlePositionsBuffer.DebugGetDataBlocking();
		}

		SetFundamentalShaderUniforms(
			particleCount, 
			voxelSize);

		BuildUnsortedVoxelParticlePairs(
			particleCount, 
			particlePositionsBuffer,
			voxelParticlePairBuffers.CurrentComputeBuffer);

		if (DebugCaptureSingleFrame)
		{
			debugUnsortedParticlePairs = voxelParticlePairBuffers.CurrentComputeBuffer.DebugGetDataBlocking();
		}

		// TODO: Sort by voxel_index.

		// TODO: Find each voxel's position in the particle indices.

		// TODO: Export: particle indices, neighborhoods

		if (DebugCaptureSingleFrame)
		{
			DebugCaptureSingleFrame = false;
			
			Debug.LogWarning("Finished debug-dumping the compute buffers. Surprised? Attach the unity debugger and breakpoint this line.");
		}
	}

	private int kernelForBuildUnsortedVoxelParticlePairs = -1;

	private PingPongComputeBuffers<SpatializerShaderVoxelParticlePair> voxelParticlePairBuffers = new PingPongComputeBuffers<SpatializerShaderVoxelParticlePair>();

	private bool TryAllocateBuffers()
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else if (MaxParticleCount <= 0)
		{
			if (DebugMessagesEnabled)
			{
				Debug.LogFormat("Buffer-allocation silently aborted because we haven't yet been given a maximum particle count.");
			}
		}
		else if (SpatializerComputeShader != null)
		{
			kernelForBuildUnsortedVoxelParticlePairs = 
				SpatializerComputeShader.FindKernel("kernel_build_unsorted_voxel_particle_pairs");

			voxelParticlePairBuffers.TryAllocateComputeBuffersWithGarbage(MaxParticleCount);
			
			if ((kernelForBuildUnsortedVoxelParticlePairs != -1) &&
				voxelParticlePairBuffers.IsInitialized)
			{
				result = true;
			}
			else
			{
				// Abort any partial-allocations.
				ReleaseBuffers();
			}
		}

		if (DebugMessagesEnabled)
		{
			Debug.LogFormat("Compute buffer allocation attempted. [Success={0}]", result);
		}

		return result;
	}

	private void ReleaseBuffers()
	{
		voxelParticlePairBuffers.ReleaseBuffers();

		if (DebugMessagesEnabled)
		{
			Debug.LogFormat("Compute buffers released.");
		}
	}

	private void SetFundamentalShaderUniforms(
		int particleCount,
		float voxelSize)
	{
		SpatializerComputeShader.SetInt("u_particle_count", particleCount);

		SpatializerComputeShader.SetInt("u_voxel_count", (VoxelsPerAxis * VoxelsPerAxis * VoxelsPerAxis));		
		SpatializerComputeShader.SetInt("u_voxel_count_per_axis", VoxelsPerAxis);
		SpatializerComputeShader.SetFloat("u_voxel_size", voxelSize);
	}

	private void BuildUnsortedVoxelParticlePairs(
		int particleCount,
		TypedComputeBuffer<SpatializerShaderParticlePosition> particlePositionsBuffer,
		TypedComputeBuffer<SpatializerShaderVoxelParticlePair> outUnsortedVoxelParticlePairsBuffer)
	{
		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_readable_sorted_voxel_particle_pairs", 
			particlePositionsBuffer);

		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_out_next_sorted_voxel_particle_pairs", 
			outUnsortedVoxelParticlePairsBuffer);
		
		ComputeShaderHelpers.DispatchLinearComputeShader(
			SpatializerComputeShader, 
			kernelForBuildUnsortedVoxelParticlePairs, 
			particleCount);
	}
}
