using System;
using UnityEngine;

// NOTE: This file reflects swarm_shader_types.cginc

// Silence warnings about the padding-fields never being accessed.
#pragma warning disable 0169, 1635

public struct SpatializerShaderParticlePosition // Represents: s_particle_position
{
	public Vector3 Position;
	
	private float Pad0; // See native-representation for padding description.

	public override string ToString()
	{
		return Position.ToString();
	}
}

public struct SpatializerShaderVoxelParticlePair // Represents: s_voxel_particle_pair
{
	public int VoxelIndex;
	public int ParticleIndex;

	public override string ToString()
	{
		return String.Format("v=[{0}], p=[{1}]", VoxelIndex, ParticleIndex);
	}
};

public struct SpatializerShaderParticleIndex // Represents: s_particle_index
{
	public int ParticleIndex;

	public override string ToString()
	{
		return String.Format("p=[{1}]", ParticleIndex);
	}
};

public struct SpatializerShaderNeighborhood // Represents: s_neighborhood
{
	// NOTE: These become an int4 in native code (to permit subscript-indexing).
	public int ParticleIndexLookupStartIndices_0;
	public int ParticleIndexLookupStartIndices_1;
	public int ParticleIndexLookupStartIndices_2;
	public int ParticleIndexLookupStartIndices_3;

	// NOTE: These become an int4 in native code (to permit subscript-indexing).
	public int ParticleIndexLookupTermIndices_0;
	public int ParticleIndexLookupTermIndices_1;
	public int ParticleIndexLookupTermIndices_2;
	public int ParticleIndexLookupTermIndices_3;

	public override string ToString()
	{
		return String.Format(
			"[{0}..{1}), [{2}..{3}), [{4}..{5}), [{6}..{7})",
			ParticleIndexLookupStartIndices_0,
			ParticleIndexLookupTermIndices_0,
			ParticleIndexLookupStartIndices_1,
			ParticleIndexLookupTermIndices_1,
			ParticleIndexLookupStartIndices_2,
			ParticleIndexLookupTermIndices_2,
			ParticleIndexLookupStartIndices_3,
			ParticleIndexLookupTermIndices_3);
	}
}

#pragma warning restore 0169, 1635
