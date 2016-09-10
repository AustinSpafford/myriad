using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SwarmSimulator : MonoBehaviour
{
	public int OrbiterCount = 1000;
	public int MaxAttractorCount = 16;

	public ComputeShader OrbitersComputeShader;
	public Material OrbitersMaterial;

	public Vector3 DebugAttractorLocalPosition = Vector3.zero;
	public float DebugAttractorAttractionScalar = 0.5f;

	public bool DebugEnabled = false;

	public void Awake()
	{
		swarmAttractorSources = GetComponents<SwarmAttractorBase>();
	}

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
			ComputeBuffer attractorsComputeBuffer;
			int activeAttractorCount;
			BuildAttractorsBuffer(
				out attractorsComputeBuffer,
				out activeAttractorCount);

			OrbitersComputeShader.SetBuffer(computeKernalIndex, "u_attractors", attractorsComputeBuffer);
			OrbitersComputeShader.SetInt("u_attractor_count", activeAttractorCount);

			OrbitersComputeShader.SetFloat("u_max_velocity_as_escape_velocity_fraction", 0.9f);
			OrbitersComputeShader.SetFloat("u_delta_time", Time.deltaTime);

			// Queue the request to permute the entire orbiters-buffer.
			{
				uint threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ;
				OrbitersComputeShader.GetKernelThreadGroupSizes(
					computeKernalIndex, 
					out threadGroupSizeX, 
					out threadGroupSizeY, 
					out threadGroupSizeZ);

				int threadsPerGroup = (int)(threadGroupSizeX * threadGroupSizeY * threadGroupSizeZ);

				int totalThreadGroupCount = 
					((orbitersComputeBuffer.count + (threadsPerGroup - 1)) / threadsPerGroup);

				OrbitersComputeShader.Dispatch(
					computeKernalIndex, 
					totalThreadGroupCount, // threadGroupsX
					1, // threadGroupsY
					1); // threadGroupsZ
			}
			
			if (OrbitersMaterial != null)
			{
				OrbitersMaterial.SetPass(0);
				OrbitersMaterial.SetBuffer("u_orbiters", orbitersComputeBuffer);
				OrbitersMaterial.SetMatrix("u_model_to_world_matrix", transform.localToWorldMatrix);
				
				int totalVertexCount = (
					orbitersComputeBuffer.count *
					OrbitersMaterial.GetInt("k_vertices_per_orbiter"));

				Graphics.DrawProcedural(MeshTopology.Points, totalVertexCount);
			}
		}
	}

	private struct ShaderAttractorState
	{
		public Vector3 Position;
		public float AttractionScalar;
	}

	private struct ShaderOrbiterState
	{
		public Vector3 Position;
		public Vector3 Velocity;
		public Vector3 Acceleration;
	}

	private const int AttractorComputeBufferCount = (2 * 2); // Double-buffered for each eye, to help avoid having SetData() cause a pipeline-stall if the data's still being read by the GPU.
	
	private Queue<ComputeBuffer> attractorsComputeBufferQueue = null;
	private ComputeBuffer orbitersComputeBuffer = null;

	private int computeKernalIndex = -1;

	private SwarmAttractorBase[] swarmAttractorSources = null;

	private List<SwarmAttractorBase.AttractorState> scratchAttractorStateList = new List<SwarmAttractorBase.AttractorState>();
	private List<ShaderAttractorState> scratchShaderAttractorStateList = new List<ShaderAttractorState>();

	private void BuildAttractorsBuffer(
		out ComputeBuffer outPooledAttractorComputeBuffer,
		out int outActiveAttractorCount)
	{
		// Grab the oldest buffer off the queue, and move it back to mark it as the most recently touched buffer.
		ComputeBuffer targetComputeBuffer = attractorsComputeBufferQueue.Dequeue();
		attractorsComputeBufferQueue.Enqueue(targetComputeBuffer);

		// Build the list of attractors.
		{
			scratchAttractorStateList.Clear();

			foreach (var swarmAttractorSource in swarmAttractorSources)
			{
				swarmAttractorSource.AppendActiveAttractors(ref scratchAttractorStateList);
			}

			if (Mathf.Approximately(DebugAttractorAttractionScalar, 0.0f) == false)
			{
				scratchAttractorStateList.Add(new SwarmAttractorBase.AttractorState()
				{
					Position = DebugAttractorLocalPosition,
					AttractionScalar = DebugAttractorAttractionScalar,
				});
			}

			if (scratchAttractorStateList.Count > targetComputeBuffer.count)
			{
				Debug.LogWarningFormat(
					"Discarding some attractors since [{0}] were wanted, but only [{1}] can be passed on.",
					scratchAttractorStateList.Count,
					targetComputeBuffer.count);

				scratchAttractorStateList.RemoveRange(
					targetComputeBuffer.count, 
					(scratchAttractorStateList.Count - targetComputeBuffer.count));
			}
		}
		
		// Convert the behavior-facing attractors into the shader's format.
		{
			scratchShaderAttractorStateList.Clear();

			Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;

			foreach (var attractorState in scratchAttractorStateList)
			{
				scratchShaderAttractorStateList.Add(new ShaderAttractorState()
				{
					Position = worldToLocalMatrix.MultiplyPoint(attractorState.Position),
					AttractionScalar = attractorState.AttractionScalar,
				});
			}
		}

		targetComputeBuffer.SetData(scratchShaderAttractorStateList.ToArray());

		outPooledAttractorComputeBuffer = targetComputeBuffer;
		outActiveAttractorCount = scratchShaderAttractorStateList.Count;
	}

	private bool TryAllocateBuffers()
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else if (OrbitersComputeShader != null)
		{
			computeKernalIndex = 
				OrbitersComputeShader.FindKernel("compute_shader_main");

			if (attractorsComputeBufferQueue == null)
			{
				attractorsComputeBufferQueue = new Queue<ComputeBuffer>(AttractorComputeBufferCount);

				for (int index = 0; index < AttractorComputeBufferCount; ++index)
				{
					attractorsComputeBufferQueue.Enqueue(
						new ComputeBuffer(
							MaxAttractorCount, 
							Marshal.SizeOf(typeof(ShaderAttractorState))));
				}

				// NOTE: There's no need to immediately initialize the buffers, since they will be populated per-frame.
			}

			if (orbitersComputeBuffer == null)
			{
				orbitersComputeBuffer =
					new ComputeBuffer(
						OrbiterCount, 
						Marshal.SizeOf(typeof(ShaderOrbiterState)));

				OrbitersComputeShader.SetBuffer(
					computeKernalIndex,
					"u_inout_orbiters",
					orbitersComputeBuffer);

				// Initialize the orbiters.
				{
					ShaderOrbiterState[] initialOrbiters = new ShaderOrbiterState[orbitersComputeBuffer.count];
				
					for (int index = 0; index < initialOrbiters.Length; ++index)
					{
						initialOrbiters[index] = new ShaderOrbiterState()
						{
							Position = (0.5f * Vector3.Scale(UnityEngine.Random.insideUnitSphere, transform.localScale)),
							Velocity = (0.05f * UnityEngine.Random.onUnitSphere),
							Acceleration = Vector3.zero,
						};
					}

					orbitersComputeBuffer.SetData(initialOrbiters);
				}
			}
			
			if ((computeKernalIndex != -1) &&
				(attractorsComputeBufferQueue != null) &&
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
			// Release all of the attractor compute buffers.
			{
				foreach (ComputeBuffer attractorComputeBuffer in attractorsComputeBufferQueue)
				{
					attractorComputeBuffer.Release();
				}

				attractorsComputeBufferQueue = null;
			}

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
