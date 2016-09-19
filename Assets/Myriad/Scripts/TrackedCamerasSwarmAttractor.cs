using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedCamerasSwarmAttractor : SwarmAttractorBase
{
	public float FalloffInnerRadius = 0.2f;
	public float FalloffOuterRadius = 0.4f;

	public float IdleAttractionScalar = -5.0f;

	public float IdleThrustScalar = 0.0f;

	public void Awake()
	{
		trackedCamera = GameObject.FindObjectOfType<SteamVR_Camera>();
	}

	public void Start()
	{
		// An empty Start() forces the inspector to add an Enabled-checkbox.
	}

	public override void AppendActiveAttractors(
		ref List<SwarmShaderAttractorState> attractors)
	{
		if (isActiveAndEnabled &&
			(trackedCamera != null) &&
			trackedCamera.isActiveAndEnabled)
		{
			var attractor = new SwarmShaderAttractorState()
			{
				Position = trackedCamera.transform.position,
				FalloffInnerRadius = this.FalloffInnerRadius,
				FalloffOuterRadius = this.FalloffOuterRadius,
				AttractionScalar = IdleAttractionScalar,
				ThrustDirection = trackedCamera.transform.forward,
				ThrustScalar = IdleThrustScalar,
			};

			attractors.Add(attractor);
		}
	}

	private SteamVR_Camera trackedCamera = null;
}
