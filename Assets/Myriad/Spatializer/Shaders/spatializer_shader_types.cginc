// Contents: Native versions of all marshalled structures for related shader.
// NOTE: This file reflects SpatializerShaderTypes.gc

struct s_particle_position // Represents: SpatializerShaderParticlePosition, which contains any usage documentation.
{
	float3 position;
	
	float pad_0; // For aligning vectors to 16-byte cache-boundaries.
};

struct s_voxel_particle_pair // Represents: SpatializerShaderVoxelParticlePair, which contains any usage documentation.
{
	int voxel_index;
	int particle_index;
};

struct s_particle_index // Represents: SpatializerShaderParticleIndex, which contains any usage documentation.
{
	int particle_index;
};

struct s_neighborhood // Represents: SpatializerShaderNeighborhood, which contains any usage documentation.
{
	int4 particle_index_lookup_start_indices;
	int4 particle_index_lookup_term_indices;
};
