using UnityEngine;
using System.Collections;

public class ComputeShaderHelpers
{
	public static void DispatchLinearComputeShader(
		ComputeShader computeShader,
		int kernelIndex,
		int threadCount)
	{
		// NOTE: The shader itself needs to avoid potential excessive-excecution in the last thread group.

		uint threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ;
		computeShader.GetKernelThreadGroupSizes(
			kernelIndex, 
			out threadGroupSizeX, 
			out threadGroupSizeY, 
			out threadGroupSizeZ);

		int threadsPerGroup = (int)(threadGroupSizeX * threadGroupSizeY * threadGroupSizeZ);

		int totalThreadGroupCount = 
			((threadCount + (threadsPerGroup - 1)) / threadsPerGroup);

		computeShader.Dispatch(
			kernelIndex, 
			totalThreadGroupCount, // threadGroupsX
			1, // threadGroupsY
			1); // threadGroupsZ
	}
}
