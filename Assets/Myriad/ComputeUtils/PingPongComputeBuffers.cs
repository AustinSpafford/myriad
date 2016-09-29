using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PingPongComputeBuffers<ElementType>
{	
	public TypedComputeBuffer<ElementType> CurrentComputeBuffer { get; private set; }
	public TypedComputeBuffer<ElementType> PreviousComputeBuffer { get; private set; }
	
	public int ElementCount
	{
		// Just to be completely paranoid, make sure we're within bounds on both the buffers.
		get { return (IsInitialized ? Math.Min(PreviousComputeBuffer.count, CurrentComputeBuffer.count) : 0); }
	}

	public bool IsInitialized
	{
		get { return ((CurrentComputeBuffer != null) && (PreviousComputeBuffer != null)); }
	}

	public bool TryAllocateComputeBuffers(
		ElementType[] initialElementValues)
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else
		{
			if (CurrentComputeBuffer == null)
			{
				CurrentComputeBuffer = new TypedComputeBuffer<ElementType>(initialElementValues.Length);
				
				CurrentComputeBuffer.SetData(initialElementValues);
			}

			if (PreviousComputeBuffer == null)
			{
				PreviousComputeBuffer = new TypedComputeBuffer<ElementType>(initialElementValues.Length);
				
				PreviousComputeBuffer.SetData(initialElementValues);
			}
			
			if ((CurrentComputeBuffer != null) &&
				(PreviousComputeBuffer != null))
			{
				result = true;
			}
		}

		return result;
	}

	public bool TryReleaseBuffers()
	{
		bool result = false;

		if (CurrentComputeBuffer != null)
		{
			CurrentComputeBuffer.Release();
			CurrentComputeBuffer = null;
			
			PreviousComputeBuffer.Release();
			PreviousComputeBuffer = null;

			result = true;
		}

		return result;
	}

	public void SwapBuffersAndBindToShaderKernel(
		ComputeShader targetShader,
		int targetKernelIndex,
		string inputBufferUniformName,
		string outputBufferUniformName,
		string elementCountUniformName)
	{
		// Swap the buffers, since when we're about to dispatch the last frame's "current" is this frame's "previous".
		{
			var temp = PreviousComputeBuffer;
			PreviousComputeBuffer = CurrentComputeBuffer;
			CurrentComputeBuffer = temp;
		}

		targetShader.SetBuffer(
			targetKernelIndex,
			inputBufferUniformName,
			PreviousComputeBuffer);
		
		targetShader.SetBuffer(
			targetKernelIndex,
			outputBufferUniformName,
			CurrentComputeBuffer);
		
		targetShader.SetInt(
			elementCountUniformName, 
			ElementCount);
	}
}
