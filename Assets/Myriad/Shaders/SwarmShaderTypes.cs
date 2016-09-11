using UnityEngine;

// NOTE: This file reflects swarm_shader_types.cginc

public struct SwarmShaderAttractorState // Represents: s_attractor_state.
{
	public Vector3 Position;
	public float FalloffInnerRadius; // Full-effect within this radius.
	public float FalloffOuterRadius; // Any effects lerp from 1.0 to 0.0 between the inner/outer radii.

	public float AttractionScalar; // Negative values will create a repulser-effect.

	public Vector3 ThrustDirection;
	public float ThrustScalar;
}

public struct SwarmShaderSwarmerState // Represents: s_swarmer_state.
{
	public Vector3 Position;
	public Vector3 Velocity;
	public Vector3 LocalUp; // For determining orientation.
}