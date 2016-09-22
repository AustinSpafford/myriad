// Contents: Any structures related to swarm-processing that would otherwise appear in multiple files.
// NOTE: This file reflects SwarmShaderTypes.gc

// Represents: ForcefieldTypeValues, which contains the documentation.
#define k_forcefield_type_global 0
#define k_forcefield_type_plane 1
#define k_forcefield_type_sphere 2
#define k_forcefield_type_thrust_capsule 3

struct s_forcefield_state // Represents: SwarmShaderForcefieldState, which contains the documentation.
{
	float4x4 simulation_to_forcefield_matrix;
	float4x4 forcefield_to_simulation_matrix;

	int forcefield_type; // Values: k_forcefield_type_*
	float forcefield_length; // See interpretation-notes in ForcefieldTypeValues.

	float falloff_inner_radius;
	float falloff_outer_radius;

	float force_scalar;
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
