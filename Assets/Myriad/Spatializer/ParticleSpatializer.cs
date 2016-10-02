using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	public int WastedParticleCount { get; private set; }

	public bool IsInitialized
	{
		get
		{
			return (
				(SpatializerComputeShader != null) &&
				(kernelForBuildUnsortedVoxelParticlePairs != -1) &&
				(kernelForAdvanceSortOfVoxelParticlePairs != -1) &&
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

	public void SetDesiredMaxParticleCount(
		int desiredMaxParticleCount)
	{
		// The bitonic sort can only operate on dataset sizes that are powers of two, so the simplest
		// workaround is to make sure we're allocating enough storage by rounding up to the next power.
		int actualMaxParticleCount = CeilingToPowerOfTwo(desiredMaxParticleCount);

		if (MaxParticleCount != actualMaxParticleCount)
		{
			MaxParticleCount = actualMaxParticleCount;

			if (isActiveAndEnabled)
			{
				ReleaseBuffers();
				TryAllocateBuffers();
			}
		}

		WastedParticleCount = (actualMaxParticleCount - desiredMaxParticleCount);
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
		List<SpatializerShaderVoxelParticlePair[]> debugVoxelParticlePairsPerSortStep = null;
		SpatializerShaderNeighborhood[] debugNeighborhoods = null;
#pragma warning restore 0219

		if (DebugCaptureSingleFrame)
		{
			debugParticlePositions = particlePositionsBuffer.DebugGetDataBlocking();
		}

		BuildUnsortedVoxelParticlePairs(
			particleCount, 
			particlePositionsBuffer,
			voxelSize);

		if (DebugCaptureSingleFrame)
		{
			debugUnsortedParticlePairs = voxelParticlePairBuffers.CurrentComputeBuffer.DebugGetDataBlocking();
			debugNeighborhoods = neighborhoodsBuffer.DebugGetDataBlocking();
		}

		SortVoxelParticlePairs(
			particleCount,
			out debugVoxelParticlePairsPerSortStep);

		// TODO: Find each voxel's position in the particle indices.

		// TODO: Export: particle indices, neighborhoods

		if (DebugCaptureSingleFrame)
		{
			DebugCaptureSingleFrame = false;
			
#pragma warning disable 0168 // Warning that variable is assigned but never referenced (these variables are for inspection in the debugger).
			var allPerParticleValues = 
				DebugParticleBufferCombinedEnumeration.ZipParticleBuffers(
					debugParticlePositions,
					debugUnsortedParticlePairs,
					debugNeighborhoods).ToArray();

			var sortStepsDebug = DebugSortStepKeys(debugVoxelParticlePairsPerSortStep).ToArray();

			Debug.Assert(DebugSortedVoxelParticlePairsAreValid(
				particleCount, 
				debugVoxelParticlePairsPerSortStep.LastOrDefault()));
#pragma warning restore 0168
			
			Debug.LogWarning("Finished debug-dumping the compute buffers. Surprised? Attach the unity debugger and breakpoint this line.");
		}

		return result;
	}

	private struct DebugParticleBufferCombinedEnumeration
	{
		public SpatializerShaderParticlePosition position;
		public SpatializerShaderVoxelParticlePair voxelParticlePair;
		public SpatializerShaderNeighborhood neighborhood;

		public override string ToString()
		{
			return String.Format(
				"{0,-23}{1,-20}{2}",
				position,
				voxelParticlePair,
				neighborhood);
		}
		
		public static IEnumerable<DebugParticleBufferCombinedEnumeration> ZipParticleBuffers(
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
					yield return new DebugParticleBufferCombinedEnumeration()
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
	private int kernelForAdvanceSortOfVoxelParticlePairs = -1;

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
			
			kernelForAdvanceSortOfVoxelParticlePairs = 
				SpatializerComputeShader.FindKernel("kernel_advance_sort_of_voxel_particle_pairs");

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

	private void BuildUnsortedVoxelParticlePairs(
		int particleCount,
		TypedComputeBuffer<SpatializerShaderParticlePosition> particlePositionsBuffer,
		float voxelSize)
	{
		SpatializerComputeShader.SetInt("u_particle_count", particleCount);

		SpatializerComputeShader.SetInt("u_voxel_count_per_axis", VoxelsPerAxis);
		SpatializerComputeShader.SetFloat("u_voxel_size", voxelSize);

		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_particle_positions", 
			particlePositionsBuffer);

		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_out_next_sorted_voxel_particle_pairs", 
			voxelParticlePairBuffers.CurrentComputeBuffer);

		SpatializerComputeShader.SetBuffer(
			kernelForBuildUnsortedVoxelParticlePairs, 
			"u_out_neighborhoods", 
			neighborhoodsBuffer);

		// NOTE: We'll initialize all particles that the sort-algorithm is going to touch.
		// Any exccess/waste particles will be initialized such that they sort to the end of the buffer.
		int sortableParticleCount = CeilingToPowerOfTwo(particleCount);

		ComputeShaderHelpers.DispatchLinearComputeShader(
			SpatializerComputeShader, 
			kernelForBuildUnsortedVoxelParticlePairs, 
			sortableParticleCount);
	}

	private void SortVoxelParticlePairs(
		int particleCount,
		out List<SpatializerShaderVoxelParticlePair[]> outDebugParticlePairsPerSortStep)
	{
		// Algorithm reference: https://en.wikipedia.org/wiki/Bitonic_sorter

		outDebugParticlePairsPerSortStep = 
			DebugCaptureSingleFrame ?
				new List<SpatializerShaderVoxelParticlePair[]>() :
				null;

		// For convenience, take a snapshot of the initial-conditions.
		if (DebugCaptureSingleFrame)
		{
			outDebugParticlePairsPerSortStep.Add(
				voxelParticlePairBuffers.CurrentComputeBuffer.DebugGetDataBlocking());
		}

		int sortableParticleCount = CeilingToPowerOfTwo(particleCount);

		// Repeatedly merge sublists of increasing size until the merged-sublist is the size of the entire buffer.
		for (int mergedSublistSizePower = 1;
			(1 << mergedSublistSizePower) <= (Int64)sortableParticleCount;
			++mergedSublistSizePower)
		{
			// To merge the sublists, start with large-scale comparison passes and refine down to comparing adjacent elements.
			for (int comparisonSublistSizePower = mergedSublistSizePower;
				comparisonSublistSizePower > 0;
				--comparisonSublistSizePower)
			{
				SpatializerComputeShader.SetInt("u_sort_comparison_group_sublist_size_power", comparisonSublistSizePower);
				SpatializerComputeShader.SetInt("u_sort_comparison_distance", (1 << (comparisonSublistSizePower - 1)));
				SpatializerComputeShader.SetInt("u_sort_direction_alternation_sublist_size_power", mergedSublistSizePower);				

				voxelParticlePairBuffers.SwapBuffersAndBindToShaderKernel(
					SpatializerComputeShader,
					kernelForAdvanceSortOfVoxelParticlePairs,
					"u_readable_sorted_voxel_particle_pairs",
					"u_out_next_sorted_voxel_particle_pairs");

				ComputeShaderHelpers.DispatchLinearComputeShader(
					SpatializerComputeShader, 
					kernelForAdvanceSortOfVoxelParticlePairs, 
					sortableParticleCount);

				if (DebugCaptureSingleFrame)
				{
					outDebugParticlePairsPerSortStep.Add(
						voxelParticlePairBuffers.CurrentComputeBuffer.DebugGetDataBlocking());
				}
			}
		}
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

	private static int CeilingToPowerOfTwo(
		int value)
	{
		return (Mathf.IsPowerOfTwo(value) ? value : Mathf.NextPowerOfTwo(value));
	}

	private static IEnumerable<string> DebugSortStepKeys(
		List<SpatializerShaderVoxelParticlePair[]> debugVoxelParticlePairsPerSortStep)
	{
		if (debugVoxelParticlePairsPerSortStep.Count > 0)
		{
			int particleCount = debugVoxelParticlePairsPerSortStep.First().Length;

			var elementHistory = new StringBuilder();

			for (int particleStorageIndex = 0;
				particleStorageIndex < particleCount;
				++particleStorageIndex)
			{
				elementHistory.Length = 0;

				for (int sortStepIndex = 0;
					sortStepIndex < debugVoxelParticlePairsPerSortStep.Count;
					++sortStepIndex)
				{
					elementHistory.AppendFormat(
						" {0,10}",
						(int)debugVoxelParticlePairsPerSortStep[sortStepIndex][particleStorageIndex].VoxelIndex);
				}

				yield return elementHistory.ToString();
			}
		}
	}

	private static bool DebugSortedVoxelParticlePairsAreValid(
		int particleCount,
		SpatializerShaderVoxelParticlePair[] debugVoxelParticlePairs)
	{
		bool result = true;

		// If any of the particle-indices were dropped (eg. stomped by a duplicate), error out.
		{
			var sortedParticleIndicesFromPairs = 
				debugVoxelParticlePairs
					.Select(element => element.ParticleIndex)
					.OrderBy(element => element)
					.Take(particleCount);

			var expectedParticleIndices = 
				Enumerable.Range(0, particleCount).Select(element => (uint)element);			
			
			if (sortedParticleIndicesFromPairs.SequenceEqual(expectedParticleIndices) == false)
			{
				result = false;
			}
		}

		// If the voxels are not in ascending order, error out.
		{
			var expectedVoxelParticlePairs = 
				debugVoxelParticlePairs.OrderBy(element => element.VoxelIndex);			

			if (debugVoxelParticlePairs.SequenceEqual(expectedVoxelParticlePairs) == false)
			{
				result = false;
			}
		}

		return result;
	}
}
