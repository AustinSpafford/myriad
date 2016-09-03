﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class OrbitersDispatcher : MonoBehaviour
{
	public int OrbiterCount = 1000;

	public Vector3 AttractorLocalPosition = Vector3.zero;
	public float AttractorGravityScalar = 0.5f;

	public ComputeShader OrbitersComputeShader;
	public Material OrbitersMaterial;

	public bool DebugEnabled = false;

	public void OnEnable()
	{
		TryAllocateBuffers();
	}

	public void OnDisable()
	{
		TryReleaseBuffers();
	}

	public void OnRenderObject()
	{
		if (OrbitersComputeShader != null)
		{			
			OrbitersComputeShader.SetVector("u_attractor_local_position", AttractorLocalPosition);
			OrbitersComputeShader.SetFloat("u_attractor_unitized_gravity", 1.0f);
			OrbitersComputeShader.SetFloat("u_max_velocity_as_escape_velocity_fraction", 0.9f);
			OrbitersComputeShader.SetFloat("u_delta_time", Time.deltaTime);

			// Queue the request permute the entire orbiters-buffer.
			{
				uint threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ;
				OrbitersComputeShader.GetKernelThreadGroupSizes(
					computeKernalIndex, 
					out threadGroupSizeX, 
					out threadGroupSizeY, 
					out threadGroupSizeZ);

				int threadsPerGroup = (int)(threadGroupSizeX * threadGroupSizeY * threadGroupSizeZ);

				int totalThreadGroupCount = 
					((OrbiterCount + (threadsPerGroup - 1)) / threadsPerGroup);

				OrbitersComputeShader.Dispatch(computeKernalIndex, totalThreadGroupCount, 1, 1);
			}
			
			if (OrbitersMaterial != null)
			{
				OrbitersMaterial.SetPass(0);
				OrbitersMaterial.SetBuffer("u_orbiters", orbitersComputeBuffer);

				int verticesPerOrbiter = OrbitersMaterial.GetInt("_VerticesPerOrbiter");
				
				int totalVertexCount = (verticesPerOrbiter * orbitersComputeBuffer.count);

				Graphics.DrawProcedural(MeshTopology.Triangles, totalVertexCount);
				
				if (DebugEnabled)
				{
					Debug.Log("Drew something!");
				}
			}
		}
	}

	private struct OrbiterState
	{
		public Vector3 Position;
		public Vector3 Velocity;
	}

	private ComputeBuffer orbitersComputeBuffer;

	private int computeKernalIndex = -1;

	private bool TryAllocateBuffers()
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else if ((OrbitersComputeShader != null) &&
			(orbitersComputeBuffer == null))
		{
			computeKernalIndex = 
				OrbitersComputeShader.FindKernel("compute_shader_main");

			orbitersComputeBuffer =
				new ComputeBuffer(
					OrbiterCount, 
					Marshal.SizeOf(typeof(OrbiterState)));

			OrbitersComputeShader.SetBuffer(
				computeKernalIndex,
				"u_inout_orbiters",
				orbitersComputeBuffer);

			// Initialize the orbiters.
			{
				OrbiterState[] initialOrbiterStates = new OrbiterState[orbitersComputeBuffer.count];
				
				for (int index = 0; index < initialOrbiterStates.Length; ++index)
				{
					initialOrbiterStates[index] = new OrbiterState()
					{
						Position = Vector3.Scale(UnityEngine.Random.insideUnitSphere, transform.localScale),
						Velocity = UnityEngine.Random.onUnitSphere,
					};
				}

				orbitersComputeBuffer.SetData(initialOrbiterStates);
			}
			
			if ((computeKernalIndex != -1) &&
				(orbitersComputeBuffer != null))
			{
				result = true;
			}
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer allocation attempted. [Success={0}]", result);
		}

		return result;
	}

	private bool TryReleaseBuffers()
	{
		bool result = false;

		if (orbitersComputeBuffer != null)
		{
			orbitersComputeBuffer.Release();
			orbitersComputeBuffer = null;

			result = true;
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer release attempted. [Success={0}]", result);
		}

		return result;
	}
}