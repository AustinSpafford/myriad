using UnityEngine;
using System.Collections;

public class ParticleSpatializer : MonoBehaviour
{
	public int CellsPerAxis = 128;

	public bool SingleFrameDebugCapture = false;

	public ComputeShader SpatializerComputeShader;

	public void BuildNeighborhoodLookupBuffers(
		TypedComputeBuffer<Vector3> particlePositionsBuffer,
		float neighborhoodRadius,
		int maximumNeighborsPerParticle,
		ref TypedComputeBuffer<int> outParticleIndicesBuffer,
		ref TypedComputeBuffer<SpatializerShaderNeighborhood> outPerParticleNeighborhoodsBuffer)
	{
		Vector3[] debugParticlePositions = null;

		if (SingleFrameDebugCapture)
		{
			debugParticlePositions = particlePositionsBuffer.DebugGetDataBlocking();
		}

		if (SingleFrameDebugCapture)
		{
			SingleFrameDebugCapture = false;
			
			Debug.LogFormat("particlePositions.Length = [{0}]", debugParticlePositions.Length);

			Debug.Break();
			Debug.Log("Finished debug-dumping the compute buffers. Surprised? Attach the unity debugger.");
		}
	}
}
