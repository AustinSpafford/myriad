using UnityEngine;
using System.Collections;

public class ParticleSpatializer : MonoBehaviour
{
	public int CellsPerAxis = 128;

	public ComputeShader SpatializerComputeShader;

	public bool DebugCaptureSingleFrame = false;

	public void BuildNeighborhoodLookupBuffers(
		TypedComputeBuffer<Vector4> particlePositionsBuffer,
		float neighborhoodRadius,
		int maximumNeighborsPerParticle,
		ref TypedComputeBuffer<int> outParticleIndicesBuffer,
		ref TypedComputeBuffer<SpatializerShaderNeighborhood> outPerParticleNeighborhoodsBuffer)
	{
		Vector4[] debugParticlePositions = null;

		if (DebugCaptureSingleFrame)
		{
			debugParticlePositions = particlePositionsBuffer.DebugGetDataBlocking();
		}

		if (DebugCaptureSingleFrame)
		{
			DebugCaptureSingleFrame = false;
			
			Debug.LogFormat("particlePositions.Length = [{0}]", debugParticlePositions.Length);
			
			Debug.LogWarning("Finished debug-dumping the compute buffers. Surprised? Attach the unity debugger and breakpoint this line.");
		}
	}
}
