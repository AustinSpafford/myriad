using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ParticleSpatializer : MonoBehaviour
{
	public struct NeighborhoodResults
	{
		// NOTE: These buffers are owned by the spatializer. Use them immediately, and do not release them.
		public TypedComputeBuffer<SpatializerShaderVoxelParticlePair> VoxelParticlePairsBuffer;	
		public TypedComputeBuffer<SpatializerShaderSpatializationVoxel> SpatializationVoxelsBuffer;
		public TypedComputeBuffer<SpatializerShaderNeighborhood> NeighborhoodsBuffer;
	}

	[Tooltip("Note that the memory/time costs are proportional to this number _cubed_!")]
	public int VoxelsPerAxis = 64;

	public ComputeShader SpatializerComputeShader;

	public bool DebugCaptureSingleFrame = false;
	public bool DebugMessagesEnabled = false;
	
	public int MaxParticleCount { get; private set; }

	public bool IsInitialized
	{
		get
		{
			return (
				(SpatializerComputeShader != null) &&
				(kernelForBuildUnsortedVoxelParticlePairs != -1) &&
				(scratchParticlePositionsBuffer != null) &&
				(neighborhoodsBuffer != null) &&
				(spatializationVoxelsBuffer != null) &&
				voxelParticlePairBuffers.IsInitialized);
		}
	}

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

	public TypedComputeBuffer<SpatializerShaderParticlePosition> GetScratchParticlePositionsComputeBuffer(
		int particleCount)
	{
		ValidateSufficientParticleCount(particleCount);

		return scratchParticlePositionsBuffer;
	}

	public NeighborhoodResults BuildNeighborhoodLookupBuffers(
		int particleCount,
		TypedComputeBuffer<SpatializerShaderParticlePosition> particlePositionsBuffer,
		float neighborhoodRadius)
	{
		var result = new NeighborhoodResults();

		ValidateSufficientParticleCount(particleCount);

		float voxelSize = (2.0f * neighborhoodRadius);
		
#pragma warning disable 0219 // Warning that variable is assigned but never referenced (these variables are for inspection in the debugger).
		SpatializerShaderParticlePosition[] debugParticlePositions = null;
		SpatializerShaderVoxelParticlePair[] debugUnsortedParticlePairs = null;
		SpatializerShaderNeighborhood[] debugNeighborhoods = null;
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
			voxelParticlePairBuffers.CurrentComputeBuffer,
			neighborhoodsBuffer);

		if (DebugCaptureSingleFrame)
		{
			debugUnsortedParticlePairs = voxelParticlePairBuffers.CurrentComputeBuffer.DebugGetDataBlocking();
			debugNeighborhoods = neighborhoodsBuffer.DebugGetDataBlocking();
		}

		// TODO: Sort by voxel_index.

		// TODO: Find each voxel's position in the particle indices.

		// TODO: Export: particle indices, neighborhoods

		if (DebugCaptureSingleFrame)
		{
			DebugCaptureSingleFrame = false;
			
#pragma warning disable 0168 // Warning that variable is assigned but never referenced (these variables are for inspection in the debugger).
			var allPerParticleValues = 
				ParticleBufferCombinedEnumeration.ZipParticleBuffers(
					debugParticlePositions,
					debugUnsortedParticlePairs,
					debugNeighborhoods).ToArray();
#pragma warning restore 0168
			
			Debug.LogWarning("Finished debug-dumping the compute buffers. Surprised? Attach the unity debugger and breakpoint this line.");
		}

		return result;
	}

	private struct ParticleBufferCombinedEnumeration
	{
		public SpatializerShaderParticlePosition position;
		public SpatializerShaderVoxelParticlePair voxelParticlePair;
		public SpatializerShaderNeighborhood neighborhood;

		public override string ToString()
		{
			return String.Format(
				"{0,-20}{1,-20}{2}",
				position,
				voxelParticlePair,
				neighborhood);
		}
		
		public static IEnumerable<ParticleBufferCombinedEnumeration> ZipParticleBuffers(
			SpatializerShaderParticlePosition[] particlePositions,
			SpatializerShaderVoxelParticlePair[] voxelParticlePairs,
			SpatializerShaderNeighborhood[] neighborhoods)
		{
			using (var particlePositionIterator = particlePositions.Cast<SpatializerShaderParticlePosition>().GetEnumerator())
			using (var voxelParticlePairIterator = voxelParticlePairs.Cast<SpatializerShaderVoxelParticlePair>().GetEnumerator())
			using (var neighborhoodsIterator = neighborhoods.Cast<SpatializerShaderNeighborhood>().GetEnumerator())
			{
				while (particlePositionIterator.MoveNext() &&
					voxelParticlePairIterator.MoveNext() &&
					neighborhoodsIterator.MoveNext())
				{
					yield return new ParticleBufferCombinedEnumeration()
					{
						position = particlePositionIterator.Current,
						voxelParticlePair = voxelParticlePairIterator.Current,
						neighborhood = neighborhoodsIterator.Current,
					};
				}
			}
		}
	}

	private int kernelForBuildUnsortedVoxelParticlePairs = -1;

	private TypedComputeBuffer<SpatializerShaderParticlePosition> scratchParticlePositionsBuffer = null;
	private TypedComputeBuffer<SpatializerShaderNeighborhood> neighborhoodsBuffer = null;
	private TypedComputeBuffer<SpatializerShaderSpatializationVoxel> spatializationVoxelsBuffer = null;
		
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

			if (scratchParticlePositionsBuffer == null)
			{
				scratchParticlePositionsBuffer = 
					new TypedComputeBuffer<SpatializerShaderParticlePosition>(MaxParticleCount);
			}	

			if (neighborhoodsBuffer == null)
			{
				neighborhoodsBuffer = 
					new TypedComputeBuffer<SpatializerShaderNeighborhood>(MaxParticleCount);
			}
			
			if (spatializationVoxelsBuffer == null)
			{
				spatializationVoxelsBuffer = 
					new TypedComputeBuffer<SpatializerShaderSpatializationVoxel>(MaxParticleCount);
			}

			voxelParticlePairBuffers.TryAllocateComputeBuffersWithGarbage(MaxParticleCount);		

			if (IsInitialized)
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
		if (scratchParticlePositionsBuffer != null)
		{
			scratchParticlePositionsBuffer.Release();
			scratchParticlePositionsBuffer = null;
		}

		if (neighborhoodsBuffer != null)
		{
			neighborhoodsBuffer.Release();
			neighborhoodsBuffer = null;
		}

		if (spatializationVoxelsBuffer != null)
		{
			spatializationVoxelsBuffer.Release();
			spatializationVoxelsBuffer = null;
		}

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

		SpatializerComputeShader.SetInt("u_voxel_count_per_axis", VoxelsPerAxis);
		SpatializerComputeShader.SetFloat("u_voxel_size", voxelSize);
	}

	private void BuildUnsortedVoxelParticlePairs(
		int particleCount,
		TypedComputeBuffer<SpatializerShaderParticlePosition> particlePositionsBuffer,
		TypedComputeBuffer<SpatializerShaderVoxelParticlePair> outUnsortedVoxelParticlePairsBuffer,
		TypedComputeBuffer<SpatializerShaderNeighborhood> outNeighborhoodsBuffer)
	{
		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_particle_positions", 
			particlePositionsBuffer);

		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_out_next_sorted_voxel_particle_pairs", 
			outUnsortedVoxelParticlePairsBuffer);

		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_out_neighborhoods", 
			outNeighborhoodsBuffer);
		
		ComputeShaderHelpers.DispatchLinearComputeShader(
			SpatializerComputeShader, 
			kernelForBuildUnsortedVoxelParticlePairs, 
			particleCount);
	}

	private void ValidateSufficientParticleCount(
		int desiredParticleCount)
	{
		if (desiredParticleCount > MaxParticleCount)
		{
			throw new InvalidOperationException(string.Format(
				"Only [{0}] particles are allocated for, but the request is for [{1}] particles. Call SetMaxParticleCount().",
				MaxParticleCount,
				desiredParticleCount));
		}
	}
}
