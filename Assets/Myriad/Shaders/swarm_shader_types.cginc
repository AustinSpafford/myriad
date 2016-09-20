// Contents: Any structures related to swarm-processing that would otherwise appear in multiple files.
// NOTE: This file reflects SwarmShaderTypes.gc

struct s_forcefield_state // Represents: SwarmShaderForcefieldState, which contains the documentation.
{
	float3 position;

	float falloff_inner_radius;
	float falloff_outer_radius;

	float attraction_scalar;
	
	float thrust_scalar;
	
	float pad_0; // For aligning vectors to 4-byte boundaries.

	float3 thrust_direction;
	
	float pad_1; // For aligning the structure-stride to a multiple of 4-bytes.
};

struct s_swarmer_state // Represents: SwarmShaderSwarmerState, which contains the documentation.
{
	float3 position;

	float pad_0; // For aligning vectors to 4-byte boundaries.

	float3 velocity;

	float pad_1; // For aligning vectors to 4-byte boundaries.

	float3 local_up;

	float pad_2; // For aligning vectors to 4-byte boundaries.

	float4x4 cached_model_to_swarm_matrix;
};

struct s_swarmer_model_vertex // Represents: SwarmShaderSwarmerModelVertex, which contains the documentation.
{
	float3 position;

	float pad_0; // For aligning vectors to 4-byte boundaries.

	float3 normal;

	float pad_1; // For aligning vectors to 4-byte boundaries.

	float4 albedo_color;
	float4 glow_color;

	float2 texture_coord;

	float left_wing_fraction;
	float right_wing_fraction;
};
