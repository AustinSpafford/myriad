using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[RequireComponent(typeof(SwarmSimulator))]
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

				Graphics.DrawProcedural(
					MeshTopology.Triangles, 
					swarmerModelVerticesBuffer.count,
					swarmersBuffer.count);
			}
		}
	}

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
		Color glowColor,
		Vector3 edgeDistances,
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
			GlowColor = glowColor,
			EdgeDistances = edgeDistances,
			LeftWingFraction = leftWingFraction,
			RightWingFraction = rightWingFraction,
		});
	}

	private static void AppendRawTriangleVerticesToModel(
		Vector3[] positions,
		bool[] renderOpposingEdge,
		Color albedoColor,
		Color glowColor,
		float leftWingFraction,
		float rightWingFraction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		if ((positions.Length != 3) ||
			(renderOpposingEdge.Length != 3))
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
		
		for (int vertexIndex = 0; vertexIndex < 3; ++vertexIndex)
		{
			// Build a vector that gives all edge-distances at this vertex's position.
			Vector3 edgeDistances = Vector3.zero;
			edgeDistances[vertexIndex] = unfilteredDistancesToOpposingEdges[vertexIndex];
			
			// NOTE: To hide any particular edge, we make it so all the vertices have a large distance to it.
			for (int edgeIndex = 0; edgeIndex < 3; ++edgeIndex)
			{
				if (renderOpposingEdge[edgeIndex] == false)
				{
					edgeDistances[edgeIndex] = 100.0f; // Force the "distance" far out beyond the model's extents.
				}
			}

			AppendVertexToModel(
				positions[vertexIndex],
				triangleNormal,
				albedoColor,
				glowColor,
				edgeDistances,
				leftWingFraction,
				rightWingFraction,
				placementMatrix,
				ref inoutSwarmerModelVertices);
		}
	}

	private static void AppendSimpleTriangleVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color glowColor,
		float leftWingFraction,
		float rightWingFraction,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		AppendRawTriangleVerticesToModel(
			positions,
			new bool[] { true, true, true }, // renderOpposingEdge
			albedoColor,
			glowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}
	
	private static void AppendSimpleQuadVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color glowColor,
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
			new Vector3[] { positions[0], positions[1], positions[3] },
			new bool[] { false, true, true }, // renderOpposingEdge, note that we're not rendering the quad's seam-edge.
			albedoColor,
			glowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		AppendRawTriangleVerticesToModel(
			new Vector3[] { positions[3], positions[1], positions[2] },
			new bool[] { true, true, false }, // renderOpposingEdge, note that we're not rendering the quad's seam-edge.
			albedoColor,
			glowColor,
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

		Vector4 centerFacetGlowColor = Color.black;

		// Top-facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { forwardPosition, leftPosition, rightPosition }, // BUG! Doesn't unity/D3D use a clockwise winding?
			(useDebugColoring ? Color.cyan : Color.yellow),
			centerFacetGlowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Bottom-facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { forwardPosition, rightPosition, leftPosition, }, // BUG! Doesn't unity/D3D use a clockwise winding?
			(useDebugColoring ? Color.white : Color.yellow),
			centerFacetGlowColor,
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
		
		Vector4 sideColor = new Color(0.4f, 0.4f, 0.7f);

		Vector4 rearColor = (useDebugColoring ? (Vector4)Color.green : sideColor);

		Vector4 frontLeftColor = (useDebugColoring ? (Vector4)Color.blue : sideColor);
		Vector4 frontRightColor = (useDebugColoring ? (Vector4)Color.red : sideColor);

		if (useTopHalfColoring == false)
		{
			Vector4 swapStorage = frontLeftColor;
			frontLeftColor = frontRightColor;
			frontRightColor = swapStorage;
		}
		
		Vector4 topFacetColor = (
			useDebugColoring ? 
				(useTopHalfColoring ? Color.yellow : Color.cyan): 
				new Color(1.0f, 0.8f, 0.3f));
		
		Vector4 disabledGlowColor = Color.black;

		// Rear-facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseLeftPosition, baseRightPosition, topRightPosition, topLeftPosition },
			rearColor,
			disabledGlowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Front-left-facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseForwardPosition, baseLeftPosition, topLeftPosition, topForwardPosition },
			frontLeftColor,
			disabledGlowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Front-right-facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseRightPosition, baseForwardPosition, topForwardPosition, topRightPosition },
			frontRightColor,
			disabledGlowColor,
			leftWingFraction,
			rightWingFraction,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Top-facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { topForwardPosition, topLeftPosition, topRightPosition },
			topFacetColor,
			disabledGlowColor,
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
