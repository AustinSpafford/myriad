// Contents: Native versions of all marshalled structures for related shader.
// NOTE: This file reflects SpatializerShaderTypes.gc

struct s_particle_position // Represents: SpatializerShaderParticlePosition, which contains any usage documentation.
{
	float3 position;
	
	float pad_0; // For aligning vector-reads to 16-byte cache-boundaries.
};

struct s_voxel_particle_pair // Represents: SpatializerShaderVoxelParticlePair, which contains any usage documentation.
{
	uint voxel_index;
	uint particle_index;
};

struct s_spatialization_voxel // Represents: SpatializerShaderSpatializationVoxel, which contains any usage documentation.
{
	uint voxel_particle_pairs_first_index;
};

struct s_neighborhood // Represents: SpatializerShaderNeighborhood, which contains any usage documentation.
{
	uint3 neighborhood_min_voxel_coord;
	
	uint pad_0; // For aligning vector-reads to 16-byte cache-boundaries.
};
