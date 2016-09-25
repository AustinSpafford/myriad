using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

[RequireComponent(typeof(SwarmForcefieldCollector))]
public class SwarmSimulator : MonoBehaviour
{
	public int SwarmerCount = 1000;
	public int MaxForcefieldCount = 16;

	public ComputeShader SwarmComputeShader;

	public bool DebugEnabled = false;

	public void Awake()
	{
		forcefieldCollector = GetComponent<SwarmForcefieldCollector>();
	}

	public void OnEnable()
	{
		TryAllocateBuffers();
	}

	public void OnDisable()
	{
		TryReleaseBuffers();
	}

	public ComputeBuffer TryBuildSwarmersForRenderFrameIndex(
		int frameIndex)
	{
		// If the swarm needs to be advanced to the requested frame.
		if ((SwarmComputeShader != null) &&
			(lastRenderedFrameIndex != frameIndex))
		{
			DateTime currentTime = DateTime.UtcNow;
			
			bool applicationIsPaused = 
#if UNITY_EDITOR
				EditorApplication.isPaused;
#else
				false;
#endif

			// The editor doesn't alter the timescale for us when the sim is paused, so we need to do it ourselves.
			float timeScale = (applicationIsPaused ? 0.0f : Time.timeScale);

			// Step ourselves based on the *graphics* framerate (since we're part of the rendering pipeline),
			// but make sure to avoid giant steps whenever rendering is paused.
			float localDeltaTime = 
				Mathf.Min(
					(float)(timeScale * (currentTime - lastRenderedDateTime).TotalSeconds),
					Time.maximumDeltaTime);

			ComputeBuffer forcefieldsComputeBuffer;
			int activeForcefieldCount;
			BuildForcefieldsBuffer(
				out forcefieldsComputeBuffer,
				out activeForcefieldCount);

			SwarmComputeShader.SetBuffer(computeKernalIndex, "u_forcefields", forcefieldsComputeBuffer);
			SwarmComputeShader.SetInt("u_forcefield_count", activeForcefieldCount);
			
			SwarmComputeShader.SetFloat("u_delta_time", localDeltaTime);

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

			lastRenderedFrameIndex = frameIndex;
			lastRenderedDateTime = currentTime;
		}

		return swarmersComputeBuffer;
	}

	private const int ForcefieldsComputeBufferCount = (2 * 2); // Double-buffered for each eye, to help avoid having SetData() cause a pipeline-stall if the data's still being read by the GPU.
	
	private SwarmForcefieldCollector forcefieldCollector = null;

	private Queue<ComputeBuffer> forcefieldsComputeBufferQueue = null;
	private ComputeBuffer swarmersComputeBuffer = null;

	private int computeKernalIndex = -1;

	private List<SwarmShaderForcefieldState> scratchForcefieldStateList = new List<SwarmShaderForcefieldState>();

	private int lastRenderedFrameIndex = -1;
	private DateTime lastRenderedDateTime = DateTime.UtcNow;

	private void BuildForcefieldsBuffer(
		out ComputeBuffer outPooledForcefieldsComputeBuffer,
		out int outActiveForcefieldCount)
	{
		// Grab the oldest buffer off the queue, and move it back to mark it as the most recently touched buffer.
		ComputeBuffer targetComputeBuffer = forcefieldsComputeBufferQueue.Dequeue();
		forcefieldsComputeBufferQueue.Enqueue(targetComputeBuffer);	

		forcefieldCollector.CollectForcefields(
			transform.localToWorldMatrix,
			ref scratchForcefieldStateList);

		if (scratchForcefieldStateList.Count > targetComputeBuffer.count)
		{
			Debug.LogWarningFormat(
				"Discarding some forcefields since [{0}] were wanted, but only [{1}] can be passed to the shader.",
				scratchForcefieldStateList.Count,
				targetComputeBuffer.count);

			scratchForcefieldStateList.RemoveRange(
				targetComputeBuffer.count, 
				(scratchForcefieldStateList.Count - targetComputeBuffer.count));
		}	

		targetComputeBuffer.SetData(scratchForcefieldStateList.ToArray());

		outPooledForcefieldsComputeBuffer = targetComputeBuffer;
		outActiveForcefieldCount = scratchForcefieldStateList.Count;
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

			if (forcefieldsComputeBufferQueue == null)
			{
				forcefieldsComputeBufferQueue = new Queue<ComputeBuffer>(ForcefieldsComputeBufferCount);

				for (int index = 0; index < ForcefieldsComputeBufferCount; ++index)
				{
					forcefieldsComputeBufferQueue.Enqueue(
						new ComputeBuffer(
							MaxForcefieldCount, 
							Marshal.SizeOf(typeof(SwarmShaderForcefieldState))));
				}

				// NOTE: There's no need to immediately initialize the buffers, since they will be populated per-frame.
			}

			if (swarmersComputeBuffer == null)
			{
				var initialSwarmers = new List<SwarmShaderSwarmerState>(SwarmerCount);
				
				for (int index = 0; index < SwarmerCount; ++index)
				{
					initialSwarmers.Add(new SwarmShaderSwarmerState()
					{
						Position = (0.5f * Vector3.Scale(UnityEngine.Random.onUnitSphere, transform.localScale)),
						Velocity = (0.05f * UnityEngine.Random.onUnitSphere), // Just a gentle nudge to indicate a direction.
						LocalUp = UnityEngine.Random.onUnitSphere,
					});
				}

				swarmersComputeBuffer =
					new ComputeBuffer(
						initialSwarmers.Count, 
						Marshal.SizeOf(initialSwarmers.GetType().GetGenericArguments()[0]));
				
				swarmersComputeBuffer.SetData(initialSwarmers.ToArray());

				SwarmComputeShader.SetBuffer(
					computeKernalIndex,
					"u_inout_swarmers",
					swarmersComputeBuffer);

				// NOTE: The shader is able to query this value, but by using this method we can
				// opt to dynamically vary the number of simulated swarmers.
				SwarmComputeShader.SetInt(
					"u_swarmer_count", 
					swarmersComputeBuffer.count);
			}
			
			if ((computeKernalIndex != -1) &&
				(forcefieldsComputeBufferQueue != null) &&
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
			// Release all of the forcefield compute buffers.
			{
				foreach (ComputeBuffer forcefieldsComputeBuffer in forcefieldsComputeBufferQueue)
				{
					forcefieldsComputeBuffer.Release();
				}

				forcefieldsComputeBufferQueue = null;
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
