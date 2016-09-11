using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SwarmSimulator : MonoBehaviour
{
	public int SwarmerCount = 1000;
	public int MaxAttractorCount = 16;

	public ComputeShader SwarmComputeShader;
	public Material SwarmMaterial;

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
		if (SwarmComputeShader != null)
		{
			ComputeBuffer attractorsComputeBuffer;
			int activeAttractorCount;
			BuildAttractorsBuffer(
				out attractorsComputeBuffer,
				out activeAttractorCount);

			SwarmComputeShader.SetBuffer(computeKernalIndex, "u_attractors", attractorsComputeBuffer);
			SwarmComputeShader.SetInt("u_attractor_count", activeAttractorCount);
			
			SwarmComputeShader.SetFloat("u_delta_time", Time.deltaTime);

			// Queue the request to permute the entire swarmers-buffer.
			{
				uint threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ;
				SwarmComputeShader.GetKernelThreadGroupSizes(
					computeKernalIndex, 
					out threadGroupSizeX, 
					out threadGroupSizeY, 
					out threadGroupSizeZ);

				int threadsPerGroup = (int)(threadGroupSizeX * threadGroupSizeY * threadGroupSizeZ);

				int totalThreadGroupCount = 
					((swarmersComputeBuffer.count + (threadsPerGroup - 1)) / threadsPerGroup);

				SwarmComputeShader.Dispatch(
					computeKernalIndex, 
					totalThreadGroupCount, // threadGroupsX
					1, // threadGroupsY
					1); // threadGroupsZ
			}
			
			if (SwarmMaterial != null)
			{
				SwarmMaterial.SetPass(0);
				SwarmMaterial.SetBuffer("u_swarmers", swarmersComputeBuffer);
				SwarmMaterial.SetMatrix("u_model_to_world_matrix", transform.localToWorldMatrix);
				
				int totalVertexCount = (
					swarmersComputeBuffer.count *
					SwarmMaterial.GetInt("k_vertices_per_swarmer"));

				Graphics.DrawProcedural(MeshTopology.Points, totalVertexCount);
			}
		}
	}

	private const int AttractorComputeBufferCount = (2 * 2); // Double-buffered for each eye, to help avoid having SetData() cause a pipeline-stall if the data's still being read by the GPU.
	
	private Queue<ComputeBuffer> attractorsComputeBufferQueue = null;
	private ComputeBuffer swarmersComputeBuffer = null;

	private int computeKernalIndex = -1;

	private SwarmAttractorBase[] swarmAttractorSources = null;

	private List<SwarmShaderAttractorState> scratchAttractorStateList = new List<SwarmShaderAttractorState>();

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
				scratchAttractorStateList.Add(new SwarmShaderAttractorState()
				{
					Position = DebugAttractorLocalPosition,
					FalloffInnerRadius = 100.0f,
					FalloffOuterRadius = 100.0f,
					AttractionScalar = DebugAttractorAttractionScalar,
					ThrustDirection = transform.forward,
					ThrustScalar = 0.0f,
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
		
		// Transform the attractors into local-space.
		{
			Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;

			for (int index = 0; index < scratchAttractorStateList.Count; ++index)
			{
				var transformedAttractorState = scratchAttractorStateList[index];

				transformedAttractorState.Position = 
					worldToLocalMatrix.MultiplyPoint(transformedAttractorState.Position);

				transformedAttractorState.ThrustDirection = 
					worldToLocalMatrix.MultiplyVector(transformedAttractorState.ThrustDirection);

				scratchAttractorStateList[index] = transformedAttractorState;
			}
		}

		targetComputeBuffer.SetData(scratchAttractorStateList.ToArray());

		outPooledAttractorComputeBuffer = targetComputeBuffer;
		outActiveAttractorCount = scratchAttractorStateList.Count;
	}

	private bool TryAllocateBuffers()
	{
		bool result = false;

		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogError("Compute shaders are not supported on this machine. Is DX11 or later installed?");
		}
		else if (SwarmComputeShader != null)
		{
			computeKernalIndex = 
				SwarmComputeShader.FindKernel("compute_shader_main");

			if (attractorsComputeBufferQueue == null)
			{
				attractorsComputeBufferQueue = new Queue<ComputeBuffer>(AttractorComputeBufferCount);

				for (int index = 0; index < AttractorComputeBufferCount; ++index)
				{
					attractorsComputeBufferQueue.Enqueue(
						new ComputeBuffer(
							MaxAttractorCount, 
							Marshal.SizeOf(typeof(SwarmShaderAttractorState))));
				}

				// NOTE: There's no need to immediately initialize the buffers, since they will be populated per-frame.
			}

			if (swarmersComputeBuffer == null)
			{
				swarmersComputeBuffer =
					new ComputeBuffer(
						SwarmerCount, 
						Marshal.SizeOf(typeof(SwarmShaderSwarmerState)));

				SwarmComputeShader.SetBuffer(
					computeKernalIndex,
					"u_inout_swarmers",
					swarmersComputeBuffer);

				// NOTE: The shader is able to query this value, but by using this method we can
				// opt to dynamically vary the number of simulated swarmers.
				SwarmComputeShader.SetInt(
					"u_swarmer_count", 
					swarmersComputeBuffer.count);

				// Initialize the swarm.
				{
					SwarmShaderSwarmerState[] initialSwarmers = new SwarmShaderSwarmerState[swarmersComputeBuffer.count];
				
					for (int index = 0; index < initialSwarmers.Length; ++index)
					{
						initialSwarmers[index] = new SwarmShaderSwarmerState()
						{
							Position = (0.5f * Vector3.Scale(UnityEngine.Random.onUnitSphere, transform.localScale)),
							Velocity = (0.05f * UnityEngine.Random.onUnitSphere), // Just a gentle nudge to indicate a direction.
							LocalUp = UnityEngine.Random.onUnitSphere,
						};
					}

					swarmersComputeBuffer.SetData(initialSwarmers);
				}
			}
			
			if ((computeKernalIndex != -1) &&
				(attractorsComputeBufferQueue != null) &&
				(swarmersComputeBuffer != null))
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

		if (swarmersComputeBuffer != null)
		{
			// Release all of the attractor compute buffers.
			{
				foreach (ComputeBuffer attractorComputeBuffer in attractorsComputeBufferQueue)
				{
					attractorComputeBuffer.Release();
				}

				attractorsComputeBufferQueue = null;
			}

			swarmersComputeBuffer.Release();
			swarmersComputeBuffer = null;

			result = true;
		}

		if (DebugEnabled)
		{
			Debug.LogFormat("Compute buffer release attempted. [Success={0}]", result);
		}

		return result;
	}
}
