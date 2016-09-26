using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class PingPongComputeBuffers<ElementType>
{	
	public ComputeBuffer CurrentComputeBuffer { get; private set; }
	public ComputeBuffer PreviousComputeBuffer { get; private set; }
	
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
		SwarmShaderSwarmerState[] initialElementValues)
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
				CurrentComputeBuffer =
					new ComputeBuffer(
						initialElementValues.Length, 
						Marshal.SizeOf(typeof(ElementType)));
				
				CurrentComputeBuffer.SetData(initialElementValues);
			}

			if (PreviousComputeBuffer == null)
			{
				PreviousComputeBuffer =
					new ComputeBuffer(
						initialElementValues.Length, 
						Marshal.SizeOf(typeof(ElementType)));
				
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

	public void SwapBuffersAndBindToShaderKernal(
		ComputeShader targetShader,
		int targetKernalIndex,
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
			targetKernalIndex,
			inputBufferUniformName,
			PreviousComputeBuffer);
		
		targetShader.SetBuffer(
			targetKernalIndex,
			outputBufferUniformName,
			CurrentComputeBuffer);
		
		targetShader.SetInt(
			elementCountUniformName, 
			ElementCount);
	}
}
