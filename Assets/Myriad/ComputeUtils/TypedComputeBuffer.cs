using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

	public void SetData(
		ElementType[] data)
	{
		computeBuffer.SetData(data);
	}

	public ElementType[] DebugGetDataBlocking()
	{
		// NOTE: This function is *terribly* slow/churning. It's just useful for crudely inspecting 
		// the contents of a ComputeBuffer without doing much plumbing-work.

		var result = new ElementType[count];

		computeBuffer.GetData(result);

		return result;
	}

	public static implicit operator ComputeBuffer(TypedComputeBuffer<ElementType> typedComputeBuffer)
	{
		return typedComputeBuffer.computeBuffer;
	}

	private ComputeBuffer computeBuffer;
}
