// Contents: Any structures related to swarm-processing that would otherwise appear in multiple files.
// NOTE: This file reflects SwarmShaderTypes.gc

struct s_attractor_state // Represents: SwarmShaderAttractorState, which contains the documentation.
{
	float3 position;

	float falloff_inner_radius;
	float falloff_outer_radius;
	
	float attraction_scalar;

	float3 thrust_direction;
	float thrust_scalar;
};

struct s_swarmer_state // Represents: SwarmShaderSwarmerState, which contains the documentation.
{
	float3 position;
	float3 velocity;
	float3 local_up;
};
