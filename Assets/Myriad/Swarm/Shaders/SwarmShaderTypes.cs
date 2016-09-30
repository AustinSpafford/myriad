using UnityEngine;

// NOTE: This file reflects swarm_shader_types.cginc

// Silence warnings about the padding-fields never being accessed.
#pragma warning disable 0169, 1635

public struct SwarmShaderForcefieldState // Represents: s_forcefield_state.
{
	public enum ForcefieldTypeValues
	{
		Global = 0, // ForcefieldLength->Ignored
		Plane = 1, // ForcefieldLength->Ignored
		Sphere = 2, // ForcefieldLength->Ignored
		ThrustCapsule = 3, // ForcefieldLength->"Length of core line-segment."
	}

	public Matrix4x4 SimulationToForcefieldMatrix;
	public Matrix4x4 ForcefieldToSimulationMatrix;

	public uint ForcefieldType; // See ForcefieldTypeValues.
	public float ForcefieldLength; // See interpretation-notes in ForcefieldTypeValues.

	public float FalloffInnerRadius; // Full-effect within this radius.
	public float FalloffOuterRadius; // Any effects lerp from 1.0 to 0.0 between the inner/outer radii.

	public float ForceScalar;
}

public struct SwarmShaderSwarmerState // Represents: s_swarmer_state.
{
	public Vector3 Position;
	
	private float Pad0; // See native-representation for padding description.

	public Vector3 Velocity;
	
	private float Pad1; // See native-representation for padding description.

	public Vector3 LocalUp; // For determining orientation.
	
	private float Pad2; // See native-representation for padding description.
	
	private Matrix4x4 CachedModelToSwarmMatrix;
}

struct SwarmShaderSwarmerModelVertex // Represents: s_swarmer_model_vertex.
{
	public Vector3 Position;

	private float Pad0; // See native-representation for padding description.

	public Vector3 Normal;
	
	private float Pad1; // See native-representation for padding description.

	public Vector4 AlbedoColor;
	public Vector4 GlowColor;
	
	public Vector2 TextureCoord;

	public float LeftWingFraction;
	public float RightWingFraction;
}

#pragma warning restore 0169, 1635
