using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public interface IAudioShaderUniformAccessor
{
	void SetColor(
		string uniformName, 
		Color color);

	void SetFloat(
		string uniformName, 
		float value);

	void SetInt(
		string uniformName, 
		int value);
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
		ComputeShader inoutComputeShader)
	{
		if (IsCollecting)
		{
			throw new InvalidOperationException();
		}

		currentCollectionComputeShader = inoutComputeShader;

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
		Material inoutMaterial)
	{
		if (IsCollecting)
		{
			throw new InvalidOperationException();
		}

		currentCollectionMaterial = inoutMaterial;

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

	void IAudioShaderUniformAccessor.SetFloat(
		string uniformName, 
		float value)
	{
		if (currentCollectionComputeShader != null)
		{
			currentCollectionComputeShader.SetFloat(uniformName, value);
		}

		if (currentCollectionMaterial != null)
		{
			currentCollectionMaterial.SetFloat(uniformName, value);
		}
	}

	void IAudioShaderUniformAccessor.SetInt(
		string uniformName, 
		int value)
	{
		if (currentCollectionComputeShader != null)
		{
			currentCollectionComputeShader.SetInt(uniformName, value);
		}

		if (currentCollectionMaterial != null)
		{
			currentCollectionMaterial.SetInt(uniformName, value);
		}
	}

	private ComputeShader currentCollectionComputeShader = null;
	private Material currentCollectionMaterial = null;
}
