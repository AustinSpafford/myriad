using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioShaderUniformCollector))]
public class AudioShaderUniformsToRenderer : MonoBehaviour
{
	public void Awake()
	{
		audioShaderUniformCollector = GetComponent<AudioShaderUniformCollector>();

		Renderer siblingRenderer = GetComponent<Renderer>();

		instancedMaterial = new Material(siblingRenderer.sharedMaterial);

		siblingRenderer.material = instancedMaterial;
	}

	public void LateUpdate()
	{
		audioShaderUniformCollector.CollectMaterialUniforms(instancedMaterial);
	}
	
	private AudioShaderUniformCollector audioShaderUniformCollector = null;
	private Material instancedMaterial = null;
}
