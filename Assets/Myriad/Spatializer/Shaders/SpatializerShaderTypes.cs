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
		return Position.ToString("n2");
	}
}

public struct SpatializerShaderVoxelParticlePair // Represents: s_voxel_particle_pair
{
	public uint VoxelIndex;
	public uint ParticleIndex;

	public override string ToString()
	{
		return String.Format("v=[{0}], p=[{1}]", (int)VoxelIndex, (int)ParticleIndex);
	}
};

public struct SpatializerShaderSpatializationVoxel // Represents: s_spatialization_voxel
{
	public uint VoxelParticlePairsFirstIndex;

	public override string ToString()
	{
		return String.Format("vpf=[{0}]", (int)VoxelParticlePairsFirstIndex);
	}
};

public struct SpatializerShaderNeighborhood // Represents: s_neighborhood
{
	// NOTE: These become an int3 in native code (unity lacks integral-vector types).
	public uint NeighborhoodMinVoxelCoord_0;
	public uint NeighborhoodMinVoxelCoord_1;
	public uint NeighborhoodMinVoxelCoord_2;
	
	private uint Pad0; // See native-representation for padding description.

	public override string ToString()
	{
		return String.Format(
			"({0}, {1}, {2})",
			(int)NeighborhoodMinVoxelCoord_0,
			(int)NeighborhoodMinVoxelCoord_1,
			(int)NeighborhoodMinVoxelCoord_2);
	}
}

#pragma warning restore 0169, 1635
