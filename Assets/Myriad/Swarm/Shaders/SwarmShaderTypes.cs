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

	public Vector3 LocalForward;
	
	public float Speed; // Note that velocity is (Speed * LocalForward).

	public Vector3 LocalUp; // Resolves the model's roll-orientation.
	
	private float Pad1; // See native-representation for padding description.

	public float LeftSegmentBendAngle;
	public float RightSegmentBendAngle;
	
	private Vector2 Pad2; // See native-representation for padding description.

	private Vector3 CachedLocalRight; // Rederived every frame from the the LocalForward and LocalUp.
	
	private float CachedDebugAcceptedCandidatesFraction; // Used to debug-visualize overcrowding conditions.
	
	private Matrix4x4 CachedModelLeftSegmentToSwarmMatrix;
	private Matrix4x4 CachedModelCenterSegmentToSwarmMatrix;
	private Matrix4x4 CachedModelRightSegmentToSwarmMatrix;
}

public struct SwarmShaderSwarmerModelVertex // Represents: s_swarmer_model_vertex.
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
