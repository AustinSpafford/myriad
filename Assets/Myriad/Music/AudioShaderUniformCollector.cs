using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public interface IAudioShaderUniformAccessor
{
	void SetColor(
		string uniformName, 
		Color color);
}

public class CollectingAudioShaderUniformsEventArgs : EventArgs
{
	public IAudioShaderUniformAccessor UniformAccessor { get; set; }
}

public class AudioShaderUniformCollector : MonoBehaviour, IAudioShaderUniformAccessor
{
	public static event EventHandler<CollectingAudioShaderUniformsEventArgs> CollectingShaderUniforms;

	public bool IsCollecting { get { return (currentCollectionComputeShader != null); } }
	
	public void CollectComputeShaderUniforms(
		ComputeShader computeShader)
	{
		if (IsCollecting)
		{
			throw new InvalidOperationException();
		}

		currentCollectionComputeShader = computeShader;

		if (CollectingShaderUniforms != null)
		{
			var eventArgs = new CollectingAudioShaderUniformsEventArgs();
			eventArgs.UniformAccessor = this;

			CollectingShaderUniforms(this, eventArgs);
		}

		currentCollectionComputeShader = null;

		if (IsCollecting)
		{
			throw new InvalidOperationException();
		}
	}
	
	public void CollectMaterialUniforms(
		Material material)
	{
		if (IsCollecting)
		{
			throw new InvalidOperationException();
		}

		currentCollectionMaterial = material;

		if (CollectingShaderUniforms != null)
		{
			var eventArgs = new CollectingAudioShaderUniformsEventArgs();
			eventArgs.UniformAccessor = this;

			CollectingShaderUniforms(this, eventArgs);
		}

		currentCollectionMaterial = null;

		if (IsCollecting)
		{
			throw new InvalidOperationException();
		}
	}

	void IAudioShaderUniformAccessor.SetColor(
		string uniformName, 
		Color color)
	{
		if (currentCollectionComputeShader != null)
		{
			currentCollectionComputeShader.SetVector(uniformName, (Vector4)color);
		}

		if (currentCollectionMaterial != null)
		{
			currentCollectionMaterial.SetColor(uniformName, color);
		}
	}

	private ComputeShader currentCollectionComputeShader = null;
	private Material currentCollectionMaterial = null;
}
