// Contents: Any structures related to swarm-processing that would otherwise appear in multiple files.

struct s_attractor_state
{
	float3 position;
	float attraction_scalar;
};

struct s_swarmer_state
{
	float3 position;
	float3 velocity;
	float3 local_up; // For determining orientation.
};
