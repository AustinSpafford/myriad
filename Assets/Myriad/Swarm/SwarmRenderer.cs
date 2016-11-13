using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[RequireComponent(typeof(AudioShaderUniformCollector))]
[RequireComponent(typeof(SwarmerModel))]
[RequireComponent(typeof(SwarmSimulator))]
public class SwarmRenderer : MonoBehaviour
{
	public Material SwarmMaterial;

	public void Awake()
	{
		audioShaderUniformCollector = GetComponent<AudioShaderUniformCollector>();
		swarmerModel = GetComponent<SwarmerModel>();
		swarmSimulator = GetComponent<SwarmSimulator>();

		// Fork the material so we avoid writing any of the
		// shader-uniform changes back to the source-material.
		SwarmMaterial = new Material(SwarmMaterial);
	}

	public void OnRenderObject()
	{
		if ((swarmSimulator != null) &&
			swarmSimulator.isActiveAndEnabled &&
			(SwarmMaterial != null))
		{
			var swarmersBuffer = swarmSimulator.TryBuildSwarmersForRenderFrameIndex(Time.renderedFrameCount);

			var swarmerModelVerticesBuffer = swarmerModel.TryCreateOrGetModelBuffer();			

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

	private AudioShaderUniformCollector audioShaderUniformCollector = null;
	private SwarmerModel swarmerModel = null;
	private SwarmSimulator swarmSimulator = null;
}
