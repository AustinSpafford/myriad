using System;
using UnityEngine;

// NOTE: This file reflects swarm_shader_types.cginc

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
}
