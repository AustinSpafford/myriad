using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SwarmerModel : MonoBehaviour
{
	public enum ModelColorationType
	{
		DebugFacetIdentification,
		ShinyFlatFacets,
	}

	public enum ModelShapeType
	{
		SimpleFlatTriangle,
		TriamondWing,
	}
	
	public ModelColorationType ModelColoration = ModelColorationType.ShinyFlatFacets;
	public ModelShapeType ModelShape = ModelShapeType.TriamondWing;

	public bool DebugLoggingEnabled = false;

	public Vector3 SwarmerCenterToRightWingtip
	{
		get
		{
			Vector3 result;

			switch (ModelShape)
			{
				case ModelShapeType.SimpleFlatTriangle:
					result = new Vector3(
						Mathf.Cos(30.0f * Mathf.Deg2Rad),
						0.0f,
						(-1.0f * Mathf.Sin(30.0f * Mathf.Deg2Rad)));
					break;

				case ModelShapeType.TriamondWing:
					result = new Vector3(
						(2.0f * Mathf.Cos(30.0f * Mathf.Deg2Rad)),
						0.0f,
						(-1.0f * Mathf.Sin(30.0f * Mathf.Deg2Rad)));
					break;

				default:
					throw new System.ComponentModel.InvalidEnumArgumentException();
			}

			return result;
		}
	}
	
	public Vector3 SwarmerLeftSegmentPivotPoint
	{
		get { return (SwarmerCenterToRightWingtip.z * Vector3.forward); }
	}
	
	public Vector3 SwarmerLeftSegmentPivotAxis
	{
		// Oriented so that positive-rotation raises the segment.
		get { return (-1.0f * (Quaternion.AngleAxis(-30.0f, Vector3.up) * Vector3.forward)); }
	}
	
	public Vector3 SwarmerRightSegmentPivotPoint
	{
		get { return SwarmerLeftSegmentPivotPoint; }
	}
	
	public Vector3 SwarmerRightSegmentPivotAxis
	{
		// Oriented so that positive-rotation raises the segment.
		get { return (-1.0f * Vector3.Reflect(SwarmerLeftSegmentPivotAxis, Vector3.right)); }
	}

	public void OnEnable()
	{
		// NOTE: We're lazy, and wait until the first model-request to allocate the buffer.
	}

	public void OnDisable()
	{
		ReleaseBuffers();
	}

	public TypedComputeBuffer<SwarmShaderSwarmerModelVertex> TryCreateOrGetModelBuffer()
	{
		if ((swarmerModelVerticesBuffer == null) ||
			(allocatedModelShapeType.Value != ModelShape) ||
			(allocatedModelColorationType.Value != ModelColoration))
		{
			ReleaseBuffers();

			TryAllocateBuffers();
		}

		return swarmerModelVerticesBuffer;
	}

	private enum FacetType
	{
		Generic,
		Front,
		Rear,
		Top,
	}

	private const float DisabledDistanceFromEdge = 100.0f; // This just needs to be large enough to be guaranteed to be well outside the model's bounds.
	
	private TypedComputeBuffer<SwarmShaderSwarmerModelVertex> swarmerModelVerticesBuffer = null;
	private ModelShapeType? allocatedModelShapeType = null;
	private ModelColorationType? allocatedModelColorationType = null;

	private static void AppendVertexToModel(
		Vector3 position,
		Vector3 normal,
		Color albedoColor,
		Color emissionColor,
		Vector4 edgeDistances,
		float leftSegmentFraction,
		float rightSegmentFraction,
		FacetType facetType,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		float frontFacetFraction = ((facetType == FacetType.Front) ? 1.0f : 0.0f);
		float rearFacetFraction = ((facetType == FacetType.Rear) ? 1.0f : 0.0f);
		float topFacetFraction = ((facetType == FacetType.Top) ? 1.0f : 0.0f);

		inoutSwarmerModelVertices.Add(new SwarmShaderSwarmerModelVertex()
		{	
			Position = placementMatrix.MultiplyPoint(position),
			Normal = placementMatrix.MultiplyVector(normal),
			AlbedoColor = albedoColor,
			EmissionColor = emissionColor,
			EdgeDistances = edgeDistances,
			LeftSegmentFraction = leftSegmentFraction,
			CenterSegmentFraction = (1.0f - (leftSegmentFraction + rightSegmentFraction)), // The segment-fractions are barycentric.
			RightSegmentFraction = rightSegmentFraction,
			GenericFacetFraction = (1.0f - (frontFacetFraction + rearFacetFraction + topFacetFraction)), // The facet-fractions are barycentric.
			FrontFacetFraction = frontFacetFraction,
			RearFacetFraction = rearFacetFraction,
			TopFacetFraction = topFacetFraction,
		});
	}

	private static void AppendRawTriangleVerticesToModel(
		Vector3[] positions,
		bool triangleIsHalfOfQuad,
		Color albedoColor,
		Color emissionColor,
		float leftSegmentFraction,
		float rightSegmentFraction,
		FacetType facetType,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		if (positions.Length != 3)
		{
			throw new System.ArgumentException();
		}

		Vector3 triangleNormal = Vector3.Cross(
			(positions[1] - positions[0]),
			(positions[2] - positions[0])).normalized;

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
				leftSegmentFraction,
				rightSegmentFraction,
				facetType,
				placementMatrix,
				ref inoutSwarmerModelVertices);
		}
	}

	private static void AppendSimpleTriangleVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color emissionColor,
		float leftSegmentFraction,
		float rightSegmentFraction,
		FacetType facetType,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		AppendRawTriangleVerticesToModel(
			positions,
			false, // triangleIsHalfOfQuad
			albedoColor,
			emissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			facetType,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}
	
	private static void AppendSimpleQuadVerticesToModel(
		Vector3[] positions,
		Color albedoColor,
		Color emissionColor,
		float leftSegmentFraction,
		float rightSegmentFraction,
		FacetType facetType,
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
			leftSegmentFraction,
			rightSegmentFraction,
			facetType,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		AppendRawTriangleVerticesToModel(
			new Vector3[] { positions[2], positions[3], positions[0] },
			true, // triangleIsHalfOfQuad
			albedoColor,
			emissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			facetType,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendFlatDoubleSidedTriangleVerticesToModel(
		float leftSegmentFraction,
		float rightSegmentFraction,
		ModelColorationType modelColoration,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		Vector3 forwardPosition = Vector3.forward;
		Vector3 rightPosition = (Quaternion.AngleAxis(120.0f, Vector3.up) * forwardPosition);
		Vector3 leftPosition = (Quaternion.AngleAxis(-120.0f, Vector3.up) * forwardPosition);

		Vector4 topFacetColor;
		Vector4 bottomFacetColor;
		switch (modelColoration)
		{
			case ModelColorationType.DebugFacetIdentification:
				topFacetColor = Color.cyan;
				bottomFacetColor = Color.red;
				break;

			case ModelColorationType.ShinyFlatFacets:
				topFacetColor = Color.yellow;
				bottomFacetColor = Color.yellow;
				break;

			default:
				throw new System.ComponentModel.InvalidEnumArgumentException();
		}
		
		Vector4 baseEmissionColor = Color.white;

		// Top facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { forwardPosition, rightPosition, leftPosition },
			topFacetColor,
			baseEmissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			FacetType.Generic,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Bottom facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { forwardPosition, leftPosition, rightPosition },
			bottomFacetColor,
			baseEmissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			FacetType.Generic,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendTriangularFrustumVerticesToModel(
		float leftSegmentFraction,
		float rightSegmentFraction,
		ModelColorationType modelColoration,
		FacetType frontLeftFacetType,
		FacetType frontRightFacetType,
		FacetType rearFacetType,
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

		Vector4 rearFacetAlbedoColor;
		Vector4 frontLeftFacetAlbedoColor;
		Vector4 frontRightFacetAlbedoColor;
		Vector4 topFacetAlbedoColor;
		switch (modelColoration)
		{
			case ModelColorationType.DebugFacetIdentification:
			{
				rearFacetAlbedoColor = Color.green;
				frontLeftFacetAlbedoColor = Color.blue;
				frontRightFacetAlbedoColor = Color.red;
				topFacetAlbedoColor = (useTopHalfColoring ? Color.yellow : Color.cyan);

				break;
			}

			case ModelColorationType.ShinyFlatFacets:
			{
				Vector4 sideColor = new Color(0.6f, 0.6f, 0.6f);

				rearFacetAlbedoColor = sideColor;
				frontLeftFacetAlbedoColor = sideColor;
				frontRightFacetAlbedoColor = sideColor;

				topFacetAlbedoColor = new Color(0.05f, 0.05f, 0.05f);

				break;
			}

			default:
				throw new System.ComponentModel.InvalidEnumArgumentException();
		}

		if (useTopHalfColoring == false)
		{
			Vector4 swapStorage = frontLeftFacetAlbedoColor;
			frontLeftFacetAlbedoColor = frontRightFacetAlbedoColor;
			frontRightFacetAlbedoColor = swapStorage;
		}

		Vector4 baseEmissionColor = Color.white;

		// Rear facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseLeftPosition, topLeftPosition, topRightPosition, baseRightPosition },
			rearFacetAlbedoColor,
			baseEmissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			rearFacetType,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Front-left facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseForwardPosition, topForwardPosition, topLeftPosition, baseLeftPosition },
			frontLeftFacetAlbedoColor,
			baseEmissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			frontLeftFacetType,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Front-right facet.
		AppendSimpleQuadVerticesToModel(
			new Vector3[] { baseRightPosition, topRightPosition, topForwardPosition, baseForwardPosition },
			frontRightFacetAlbedoColor,
			baseEmissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			frontRightFacetType,
			placementMatrix,
			ref inoutSwarmerModelVertices);

		// Top facet.
		AppendSimpleTriangleVerticesToModel(
			new Vector3[] { topForwardPosition, topRightPosition, topLeftPosition },
			topFacetAlbedoColor,
			baseEmissionColor,
			leftSegmentFraction,
			rightSegmentFraction,
			FacetType.Top,
			placementMatrix,
			ref inoutSwarmerModelVertices);
	}

	private static void AppendTriangularBifrustumVerticesToModel(
		float leftSegmentFraction,
		float rightSegmentFraction,
		ModelColorationType modelColoration,
		FacetType frontLeftFacetType,
		FacetType frontRightFacetType,
		FacetType rearFacetType,
		Matrix4x4 placementMatrix,
		ref List<SwarmShaderSwarmerModelVertex> inoutSwarmerModelVertices)
	{
		// Top-half.
		AppendTriangularFrustumVerticesToModel(
			leftSegmentFraction,
			rightSegmentFraction,
			modelColoration,
			frontLeftFacetType,
			frontRightFacetType,
			rearFacetType,
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
				leftSegmentFraction,
				rightSegmentFraction,
				modelColoration,
				frontRightFacetType, // NOTE: Left-right swapped.
				frontLeftFacetType, // NOTE: Left-right swapped.
				rearFacetType,
				false, // useTopHalfColoring
				(placementMatrix * flippingMatrix),
				ref inoutSwarmerModelVertices);
		}
	}

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

				switch (ModelShape)
				{
					case ModelShapeType.SimpleFlatTriangle:
					{
						AppendFlatDoubleSidedTriangleVerticesToModel(
							0.0f, // leftSegmentFraction
							0.0f, // rightSegmentFraction
							ModelColoration,
							Matrix4x4.identity,
							ref swarmerModelVertices);

						break;
					}

					case ModelShapeType.TriamondWing:
					{
						AppendTriangularBifrustumVerticesToModel(
							0.0f, // leftSegmentFraction
							0.0f, // rightSegmentFraction
							ModelColoration,
							FacetType.Generic, // Front-left (before rotation).
							FacetType.Generic, // Front-right (before rotation).
							FacetType.Front, // Rear (before rotation).
							Matrix4x4.TRS(
								new Vector3(
									0.0f, 
									0.0f, 
									(1.0f - Mathf.Sin(30.0f * Mathf.Deg2Rad))),
								Quaternion.AngleAxis(180, Vector3.up),
								Vector3.one),
							ref swarmerModelVertices);

						AppendTriangularBifrustumVerticesToModel(
							0.0f, // leftSegmentFraction
							1.0f, // rightSegmentFraction
							ModelColoration,
							FacetType.Generic, // Front-left (before rotation).
							FacetType.Front, // Front-right (before rotation).
							FacetType.Rear, // Rear (before rotation).
							Matrix4x4.TRS(
								new Vector3(
									(1.0f * Mathf.Cos(30.0f * Mathf.Deg2Rad)), 
									0.0f, 
									0.0f),
								Quaternion.identity,
								Vector3.one),
							ref swarmerModelVertices);

						AppendTriangularBifrustumVerticesToModel(
							1.0f, // leftSegmentFraction
							0.0f, // rightSegmentFraction
							ModelColoration,
							FacetType.Front, // Front-left (before rotation).
							FacetType.Generic, // Front-right (before rotation).
							FacetType.Rear, // Rear (before rotation).
							Matrix4x4.TRS(
								new Vector3(
									(-1.0f * Mathf.Cos(30.0f * Mathf.Deg2Rad)), 
									0.0f, 
									0.0f),
								Quaternion.identity,
								Vector3.one),
							ref swarmerModelVertices);
							
						break;
					}

					default:
						throw new System.ComponentModel.InvalidEnumArgumentException();
				}

				swarmerModelVerticesBuffer =
					new TypedComputeBuffer<SwarmShaderSwarmerModelVertex>(swarmerModelVertices.Count);
				
				swarmerModelVerticesBuffer.SetData(swarmerModelVertices.ToArray());

				allocatedModelColorationType = ModelColoration;
				allocatedModelShapeType = ModelShape;
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

		if (DebugLoggingEnabled)
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

			allocatedModelColorationType = null;
			allocatedModelShapeType = null;
		}

		if (DebugLoggingEnabled)
		{
			Debug.LogFormat("Compute buffers released.");
		}
	}
}
