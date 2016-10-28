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
	
	public float DebugAcceptedCandidatesFraction;
	
	private Matrix4x4 CachedModelToSwarmMatrix;
}

struct SwarmShaderSwarmerModelVertex // Represents: s_swarmer_model_vertex.
{
	public Vector3 Position;

	private float Pad0; // See native-representation for padding description.

	public Vector3 Normal;
	
	private float Pad1; // See native-representation for padding description.

	public Vector4 AlbedoColor;
	public Vector4 EmissionColor;
	
	public Vector4 EdgeDistances;

	public float LeftSegmentFraction;
	public float CenterSegmentFraction;
	public float RightSegmentFraction;
	
	public float GenericFacetFraction;
	public float FrontFacetFraction;
	public float RearFacetFraction;
	public float TopFacetFraction;
	
	private float Pad2; // See native-representation for padding description.
}

#pragma warning restore 0169, 1635
