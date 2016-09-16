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

	public void OnRenderObject()
	{
		if (DebugEnabled)
		{
			Debug.LogFormat("OnRenderObject at frame [{0}].", Time.renderedFrameCount);
		}
			
		if ((swarmSimulator != null) &&
			swarmSimulator.isActiveAndEnabled &&
			(SwarmMaterial != null))
		{
			ComputeBuffer swarmersComputeBuffer = 
				swarmSimulator.TryBuildSwarmersForRenderFrameIndex(
					Time.renderedFrameCount);
			
			if (swarmersComputeBuffer != null)
			{
				SwarmMaterial.SetPass(0);
				SwarmMaterial.SetBuffer("u_swarmers", swarmersComputeBuffer);
				SwarmMaterial.SetMatrix("u_model_to_world_matrix", transform.localToWorldMatrix);
				
				int totalVertexCount = (
					swarmersComputeBuffer.count *
					SwarmMaterial.GetInt("k_vertices_per_swarmer"));

				Graphics.DrawProcedural(MeshTopology.Points, totalVertexCount);
			}
		}
	}

	private SwarmSimulator swarmSimulator = null;
}
