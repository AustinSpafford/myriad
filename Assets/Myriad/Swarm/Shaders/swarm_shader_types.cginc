// Contents: Native versions of all marshalled structures for related shader.
// NOTE: This file reflects SwarmShaderTypes.gc

// Represents: ForcefieldTypeValues, which contains the documentation.
#define k_forcefield_type_global 0
#define k_forcefield_type_plane 1
#define k_forcefield_type_sphere 2
#define k_forcefield_type_thrust_capsule 3

struct s_forcefield_state // Represents: SwarmShaderForcefieldState, which contains any usage documentation.
{
	float4x4 simulation_to_forcefield_matrix;
	float4x4 forcefield_to_simulation_matrix;

	uint forcefield_type; // Values: k_forcefield_type_*
	float forcefield_length; // See interpretation-notes in ForcefieldTypeValues.

	float falloff_inner_radius;
	float falloff_outer_radius;

	float force_scalar;
};

struct s_swarmer_state // Represents: SwarmShaderSwarmerState, which contains any usage documentation.
{
	float3 position;

	float pad_0; // For aligning vector-reads to 16-byte cache-boundaries.

	float3 local_forward;

	float speed;

	float3 local_up;

	float pad_1; // For aligning vector-reads to 16-byte cache-boundaries.

	float steering_left_segment_bend_angle;
	float steering_right_segment_bend_angle;

	float swim_cycle_fraction;

	float pad_2; // For aligning vector-reads to 16-byte cache-boundaries.

	float3 cached_local_right;
	
	float cached_debug_accepted_candidates_fraction;

	float4x4 cached_model_left_segment_to_swarm_matrix;
	float4x4 cached_model_center_segment_to_swarm_matrix;
	float4x4 cached_model_right_segment_to_swarm_matrix;
};

struct s_swarmer_model_vertex // Represents: SwarmShaderSwarmerModelVertex, which contains any usage documentation.
{
	float3 position;

	float pad_0; // For aligning vector-reads to 16-byte cache-boundaries.

	float3 normal;

	float pad_1; // For aligning vector-reads to 16-byte cache-boundaries.

	float4 albedo_color;
	float4 emission_color;

	float4 edge_distances;

	float left_segment_fraction;
	float center_segment_fraction;
	float right_segment_fraction;	
	
	float generic_facet_fraction;
	float front_facet_fraction;
	float rear_facet_fraction;
	float top_facet_fraction;

	float pad_2; // For aligning vector-reads to 16-byte cache-boundaries.
};
