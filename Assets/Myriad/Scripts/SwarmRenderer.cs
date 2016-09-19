using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SwarmRenderer : MonoBehaviour
{
	public Material SwarmMaterial;

	public bool DebugEnabled = false;

	public void Awake()
	{
		swarmSimulator = GetComponent<SwarmSimulator>();
	}

	public void OnEnable()
	{
		TryAllocateBuffers();
	}

	public void OnDisable()
	{
		TryReleaseBuffers();
	}

	public void OnRenderObject()
	{
		if ((swarmSimulator != null) &&
			swarmSimulator.isActiveAndEnabled &&
			(SwarmMaterial != null))
		{
			ComputeBuffer swarmersComputeBuffer = 
				swarmSimulator.TryBuildSwarmersForRenderFrameIndex(
					Time.renderedFrameCount);
			
			if ((swarmersComputeBuffer != null) &&
				(swarmerModelVerticesComputeBuffer != null))
			{
				SwarmMaterial.SetPass(0);
				SwarmMaterial.SetBuffer("u_swarmers", swarmersComputeBuffer);
				SwarmMaterial.SetBuffer("u_swarmer_model_vertices", swarmerModelVerticesComputeBuffer);
				SwarmMaterial.SetMatrix("u_swarm_to_world_matrix", transform.localToWorldMatrix);

				Graphics.DrawProcedural(
					MeshTopology.Triangles, 
					swarmerModelVerticesComputeBuffer.count,
					swarmersComputeBuffer.count);
			}
		}
	}

	private SwarmSimulator swarmSimulator = null;
	
	private ComputeBuffer swarmerModelVerticesComputeBuffer = null;

	private static void AppendVertexToModel(
		Vector3 position,
		Vector3 normal,
		Color albedoColor,
		Color glowColor,
		Vector2 textureCoord,
		float leftWingFraction,
		float rightWingFaction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		inoutSwarmerModelVertices.Add(new SwarmShaderSwarmerModelVertex()
		{	
			Position = placementMatrix.MultiplyPoint(position),
			Normal = placementMatrix.MultiplyVector(normal),
			AlbedoColor = albedoColor,
			GlowColor = glowColor,
			TextureCoord = textureCoord,
			LeftWingFraction = leftWingFraction,
			RightWingFraction = rightWingFaction,
		});
	}

	private static void AppendTriangleVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color glowColor,
		float leftWingFraction,
		float rightWingFaction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		if (positions.Length != 3)
		{
			throw new System.ArgumentException();
		}

		Vector3 triangleNormal = Vector3.Cross(
			positions[1] - positions[0],
			positions[2] - positions[0]).normalized;

		AppendVertexToModel(
			positions[0],
			triangleNormal,
			albedoColor,
			glowColor,
			new Vector2(0.5f, 1.0f), // TODO: Either clean this up, or remove the texture coords altogether.
			leftWingFraction,
			rightWingFaction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		AppendVertexToModel(
			positions[1],
			triangleNormal,
			albedoColor,
			glowColor,
			new Vector2(1.0f, 0.0f), // TODO: Either clean this up, or remove the texture coords altogether.
			leftWingFraction,
			rightWingFaction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		AppendVertexToModel(
			positions[2],
			triangleNormal,
			albedoColor,
			glowColor,
			new Vector2(0.0f, 0.0f), // TODO: Either clean this up, or remove the texture coords altogether.
			leftWingFraction,
			rightWingFaction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendPuffedTriangleVerticesToModel(
		float leftWingFraction,
		float rightWingFraction,
		bool useDebugColoring,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		Vector3 forwardPosition = new Vector3(0.0f, 0.0f, 1.0f);
		Vector3 rightPosition = new Vector3(1.0f, 0.0f, -1.0f);
		Vector3 leftPosition = new Vector3(-1.0f, 0.0f, -1.0f);

		Vector4 centerFacetGlowColor = Color.black;

		// Top-facet.
		AppendTriangleVerticesToModel(
			new Vector3[] { forwardPosition, leftPosition, rightPosition }, // BUG! Doesn't unity/D3D use a clockwise winding?
			(useDebugColoring ? Color.cyan : Color.yellow),
			centerFacetGlowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Bottom-facet.
		AppendTriangleVerticesToModel(
			new Vector3[] { forwardPosition, rightPosition, leftPosition, }, // BUG! Doesn't unity/D3D use a clockwise winding?
			(useDebugColoring ? Color.white : Color.yellow),
			centerFacetGlowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}
	
	private bool TryAllocateBuffers()
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else
		{
			if (swarmerModelVerticesComputeBuffer == null)
			{
				var swarmerModelVertices = new List<SwarmShaderSwarmerModelVertex>();

				bool useDebugColoring = true; // TODO: Expose this and handle it changing on the fly.

				AppendPuffedTriangleVerticesToModel(
					0.0f, // leftWingFraction
					0.0f, // rightWingFraction
					useDebugColoring,
					Matrix4x4.identity,
					ref swarmerModelVertices);

				swarmerModelVerticesComputeBuffer =
					new ComputeBuffer(
						swarmerModelVertices.Count,
						Marshal.SizeOf(swarmerModelVertices.GetType().GetGenericArguments()[0]));
				
				swarmerModelVerticesComputeBuffer.SetData(swarmerModelVertices.ToArray());
			}
			
			if (swarmerModelVerticesComputeBuffer != null)
			{
				result = true;
			}
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer allocation attempted. [Success={0}]", result);
		}

		return result;
	}

	private bool TryReleaseBuffers()
	{
		bool result = false;

		if (swarmerModelVerticesComputeBuffer != null)
		{
			swarmerModelVerticesComputeBuffer.Release();
			swarmerModelVerticesComputeBuffer = null;

			result = true;
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer release attempted. [Success={0}]", result);
		}

		return result;
	}
}
