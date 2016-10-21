using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[RequireComponent(typeof(SwarmSimulator))]
[RequireComponent(typeof(AudioShaderUniformCollector))]
public class SwarmRenderer : MonoBehaviour
{
	public Material SwarmMaterial;

	public bool DebugEnabled = false;

	public void Awake()
	{
		audioShaderUniformCollector = GetComponent<AudioShaderUniformCollector>();
		swarmSimulator = GetComponent<SwarmSimulator>();

		// Fork the material so we avoid writing any of the
		// shader-uniform changes back to the source-material.
		SwarmMaterial = new Material(SwarmMaterial);
	}

	public void OnEnable()
	{
		TryAllocateBuffers();
	}

	public void OnDisable()
	{
		ReleaseBuffers();
	}

	public void OnRenderObject()
	{
		if ((swarmSimulator != null) &&
			swarmSimulator.isActiveAndEnabled &&
			(SwarmMaterial != null))
		{
			TypedComputeBuffer<SwarmShaderSwarmerState> swarmersBuffer = 
				swarmSimulator.TryBuildSwarmersForRenderFrameIndex(
					Time.renderedFrameCount);
			
			if ((swarmersBuffer != null) &&
				(swarmerModelVerticesBuffer != null))
			{
				SwarmMaterial.SetPass(0);
				SwarmMaterial.SetBuffer("u_swarmers", swarmersBuffer);
				SwarmMaterial.SetBuffer("u_swarmer_model_vertices", swarmerModelVerticesBuffer);
				SwarmMaterial.SetMatrix("u_swarm_to_world_matrix", transform.localToWorldMatrix);

				audioShaderUniformCollector.CollectMaterialUniforms(SwarmMaterial);

				Graphics.DrawProcedural(
					MeshTopology.Triangles, 
					swarmerModelVerticesBuffer.count,
					swarmersBuffer.count);
			}
		}
	}

	private const float DisabledDistanceFromEdge = 100.0f; // This just needs to be large enough to be guaranteed to be well outside the model's bounds.
	
	private AudioShaderUniformCollector audioShaderUniformCollector = null;
	private SwarmSimulator swarmSimulator = null;
	
	private TypedComputeBuffer<SwarmShaderSwarmerModelVertex> swarmerModelVerticesBuffer = null;

	private static float DistanceToLine(
		Vector3 subjectPoint,
		Vector3 linePointAlpha,
		Vector3 linePointBravo)
	{
		Vector3 lineToSubjectDelta = (subjectPoint - linePointAlpha);

		return Vector3.Distance(
			lineToSubjectDelta,
			Vector3.Project(
				lineToSubjectDelta, 
				(linePointBravo - linePointAlpha)));
	}

	private static void AppendVertexToModel(
		Vector3 position,
		Vector3 normal,
		Color albedoColor,
		Color emissionColor,
		Vector4 edgeDistances,
		float leftWingFraction,
		float rightWingFraction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		inoutSwarmerModelVertices.Add(new SwarmShaderSwarmerModelVertex()
		{	
			Position = placementMatrix.MultiplyPoint(position),
			Normal = placementMatrix.MultiplyVector(normal),
			AlbedoColor = albedoColor,
			EmissionColor = emissionColor,
			EdgeDistances = edgeDistances,
			LeftWingFraction = leftWingFraction,
			RightWingFraction = rightWingFraction,
		});
	}

	private static void AppendRawTriangleVerticesToModel(
		Vector3[] positions,
		bool triangleIsHalfOfQuad,
		Color albedoColor,
		Color emissionColor,
		float leftWingFraction,
		float rightWingFraction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		if (positions.Length != 3)
		{
			throw new System.ArgumentException();
		}

		Vector3 triangleNormal = Vector3.Cross(
			positions[2] - positions[0],
			positions[1] - positions[0]).normalized;

		// NOTE: If we ever start distorting the model's faces, it might be worthwhile to 
		// move this math down into the vertex-shader so it stays accurate.
		Vector3 unfilteredDistancesToOpposingEdges = new Vector3(
			DistanceToLine(positions[0], positions[1], positions[2]),
			DistanceToLine(positions[1], positions[2], positions[0]),
			DistanceToLine(positions[2], positions[0], positions[1]));

		// NOTE: For the "fourth edgeDistance" to work, the quad has to be wider than tall, and with 
		// with parallel first and third edges. If those assumptions fail a small visual artifact appears
		// where the quad's two triangles meet.
		Vector4[] perVertexEdgeDistances = new Vector4[] {
			new Vector4(
				unfilteredDistancesToOpposingEdges[0],
				(triangleIsHalfOfQuad ? DisabledDistanceFromEdge : 0), // Erase the quad's shared-edge by never approaching zero.
				0,
				(triangleIsHalfOfQuad ? unfilteredDistancesToOpposingEdges[2] : DisabledDistanceFromEdge)), // Fake an edge outside the triangle that's parallel to our first.
			new Vector4(
				0,
				(triangleIsHalfOfQuad ? DisabledDistanceFromEdge : unfilteredDistancesToOpposingEdges[1]), // Erase the quad's shared-edge by never approaching zero.
				0,
				(triangleIsHalfOfQuad ? unfilteredDistancesToOpposingEdges[2] : DisabledDistanceFromEdge)), // Fake an edge outside the triangle that's parallel to our first.
			new Vector4(
				0,
				(triangleIsHalfOfQuad ? DisabledDistanceFromEdge : 0), // Erase the quad's shared-edge by never approaching zero.
				unfilteredDistancesToOpposingEdges[2],
				(triangleIsHalfOfQuad ? 0 : DisabledDistanceFromEdge)), // Fake an edge outside the triangle that's parallel to our first.
		};
		
		for (int vertexIndex = 0; vertexIndex < 3; ++vertexIndex)
		{
			AppendVertexToModel(
				positions[vertexIndex],
				triangleNormal,
				albedoColor,
				emissionColor,
				perVertexEdgeDistances[vertexIndex],
				leftWingFraction,
				rightWingFraction,
				placementMatrix,
				ref inoutSwarmerModelVertices);
		}
	}

	private static void AppendSimpleTriangleVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color emissionColor,
		float leftWingFraction,
		float rightWingFraction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		AppendRawTriangleVerticesToModel(
			positions,
			false, // triangleIsHalfOfQuad
			albedoColor,
			emissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}
	
	private static void AppendSimpleQuadVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color emissionColor,
		float leftWingFraction,
		float rightWingFraction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		if (positions.Length != 4)
		{
			throw new System.ArgumentException();
		}

		AppendRawTriangleVerticesToModel(
			new Vector3[] { positions[0], positions[1], positions[2] },
			true, // triangleIsHalfOfQuad
			albedoColor,
			emissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		AppendRawTriangleVerticesToModel(
			new Vector3[] { positions[2], positions[3], positions[0] },
			true, // triangleIsHalfOfQuad
			albedoColor,
			emissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendFlatDoubleSidedTriangleVerticesToModel(
		float leftWingFraction,
		float rightWingFraction,
		bool useDebugColoring,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		Vector3 forwardPosition = new Vector3(0.0f, 0.0f, 1.0f);
		Vector3 rightPosition = new Vector3(1.0f, 0.0f, -1.0f);
		Vector3 leftPosition = new Vector3(-1.0f, 0.0f, -1.0f);

		Vector4 centerFacetEmissionColor = Color.black;

		// Top-facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { forwardPosition, leftPosition, rightPosition }, // BUG! Doesn't unity/D3D use a clockwise winding?
			(useDebugColoring ? Color.cyan : Color.yellow),
			centerFacetEmissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Bottom-facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { forwardPosition, rightPosition, leftPosition, }, // BUG! Doesn't unity/D3D use a clockwise winding?
			(useDebugColoring ? Color.white : Color.yellow),
			centerFacetEmissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendTriangularFrustumVerticesToModel(
		float leftWingFraction,
		float rightWingFraction,
		bool useDebugColoring,
		bool useTopHalfColoring,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		float bevelSizeY = 0.2f;
		float bevelSizeZ = 0.6f;

		Vector3 baseForwardPosition = Vector3.forward;
		Vector3 baseRightPosition = (Quaternion.AngleAxis(120.0f, Vector3.up) * baseForwardPosition);
		Vector3 baseLeftPosition = (Quaternion.AngleAxis(-120.0f, Vector3.up) * baseForwardPosition);

		Vector3 topForwardPosition = (baseForwardPosition + new Vector3(0.0f, bevelSizeY, (-1.0f * bevelSizeZ)));
		Vector3 topRightPosition = (Quaternion.AngleAxis(120.0f, Vector3.up) * topForwardPosition);
		Vector3 topLeftPosition = (Quaternion.AngleAxis(-120.0f, Vector3.up) * topForwardPosition);
		
		Vector4 sideColor = new Color(0.6f, 0.6f, 0.6f);

		Vector4 rearColor = (useDebugColoring ? (Vector4)Color.green : sideColor);

		Vector4 frontLeftColor = (useDebugColoring ? (Vector4)Color.blue : sideColor);
		Vector4 frontRightColor = (useDebugColoring ? (Vector4)Color.red : sideColor);

		if (useTopHalfColoring == false)
		{
			Vector4 swapStorage = frontLeftColor;
			frontLeftColor = frontRightColor;
			frontRightColor = swapStorage;
		}
		
		Vector4 topFacetAlbedoColor = (
			useDebugColoring ? 
				(useTopHalfColoring ? Color.yellow : Color.cyan): 
				new Color(0.1f, 0.1f, 0.1f));

		Vector4 topFacetEmissionColor = new Color(1.0f, 0.8f, 0.1f);
		
		Vector4 disabledEmissionColor = Color.black;

		// Rear-facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseLeftPosition, baseRightPosition, topRightPosition, topLeftPosition },
			rearColor,
			disabledEmissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Front-left-facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseForwardPosition, baseLeftPosition, topLeftPosition, topForwardPosition },
			frontLeftColor,
			disabledEmissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Front-right-facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseRightPosition, baseForwardPosition, topForwardPosition, topRightPosition },
			frontRightColor,
			disabledEmissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Top-facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { topForwardPosition, topLeftPosition, topRightPosition },
			topFacetAlbedoColor,
			topFacetEmissionColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendTriangularBifrustumVerticesToModel(
		float leftWingFraction,
		float rightWingFraction,
		bool useDebugColoring,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		// Top-half.
		AppendTriangularFrustumVerticesToModel(
			leftWingFraction,
			rightWingFraction,
			useDebugColoring,
			true, // useTopHalfColoring
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Bottom-half.
		{
			Matrix4x4 flippingMatrix = Matrix4x4.TRS(
				Vector3.zero,
				Quaternion.AngleAxis(180.0f, Vector3.forward),
				Vector3.one);

			AppendTriangularFrustumVerticesToModel(
				leftWingFraction,
				rightWingFraction,
				useDebugColoring,
				false, // useTopHalfColoring
				(placementMatrix * flippingMatrix),
				ref inoutSwarmerModelVertices);
		}
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
			if (swarmerModelVerticesBuffer == null)
			{
				var swarmerModelVertices = new List<SwarmShaderSwarmerModelVertex>();

				bool useDebugColoring = false; // TODO: Expose this and handle it changing on the fly.

				bool useSimpleFlatTriangleModel = false; // TODO: Remove this entirely once performance comparisons have been made against the old geometry-shader approach.

				if (useSimpleFlatTriangleModel)
				{
					AppendFlatDoubleSidedTriangleVerticesToModel(
						0.0f, // leftWingFraction
						0.0f, // rightWingFraction
						useDebugColoring,
						Matrix4x4.identity,
						ref swarmerModelVertices);
				}
				else
				{
					AppendTriangularBifrustumVerticesToModel(
						0.0f, // leftWingFraction
						0.0f, // rightWingFraction
						useDebugColoring,
						Matrix4x4.TRS(
							new Vector3(
								0.0f, 
								0.0f, 
								(1.0f - Mathf.Sin(30.0f * Mathf.Deg2Rad))),
							Quaternion.AngleAxis(180, Vector3.up),
							Vector3.one),
						ref swarmerModelVertices);

					AppendTriangularBifrustumVerticesToModel(
						0.0f, // leftWingFraction
						1.0f, // rightWingFraction
						useDebugColoring,
						Matrix4x4.TRS(
							new Vector3(
								Mathf.Cos(30.0f * Mathf.Deg2Rad), 
								0.0f, 
								0.0f),
							Quaternion.identity,
							Vector3.one),
						ref swarmerModelVertices);

					AppendTriangularBifrustumVerticesToModel(
						1.0f, // leftWingFraction
						0.0f, // rightWingFraction
						useDebugColoring,
						Matrix4x4.TRS(
							new Vector3(
								(-1.0f * Mathf.Cos(30.0f * Mathf.Deg2Rad)), 
								0.0f, 
								0.0f),
							Quaternion.identity,
							Vector3.one),
						ref swarmerModelVertices);
				}

				swarmerModelVerticesBuffer =
					new TypedComputeBuffer<SwarmShaderSwarmerModelVertex>(swarmerModelVertices.Count);
				
				swarmerModelVerticesBuffer.SetData(swarmerModelVertices.ToArray());
			}
			
			if (swarmerModelVerticesBuffer != null)
			{
				result = true;
			}
			else
			{
				// Abort any partial-allocations.
				ReleaseBuffers();
			}
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer allocation attempted. [Success={0}]", result);
		}

		return result;
	}

	private void ReleaseBuffers()
	{
		if (swarmerModelVerticesBuffer != null)
		{
			swarmerModelVerticesBuffer.Release();
			swarmerModelVerticesBuffer = null;
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffers released.");
		}
	}
}
