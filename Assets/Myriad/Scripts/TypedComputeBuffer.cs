using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

// This typing-wrapper around ComputeBuffer is meant to provide rigid documentation regarding the contents of such buffers.
public class TypedComputeBuffer<ElementType>
{
	public int count { get { return computeBuffer.count; } }
	public int stride { get { return computeBuffer.stride; } }

	public TypedComputeBuffer(
		int elementCount)
	{
		computeBuffer = 
			new ComputeBuffer(
				elementCount, 
				Marshal.SizeOf(typeof(ElementType)));
	}

	public void Release()
	{
		computeBuffer.Release();
		computeBuffer = null;
	}

	public void GetData(
		ElementType[] data)
	{
		computeBuffer.GetData(data);
	}

	public void SetData(
		ElementType[] data)
	{
		computeBuffer.SetData(data);
	}

	public static implicit operator ComputeBuffer(TypedComputeBuffer<ElementType> typedComputeBuffer)
	{
		return typedComputeBuffer.computeBuffer;
	}

	private ComputeBuffer computeBuffer;
}
