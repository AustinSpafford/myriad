using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedCamerasSwarmAttractor : SwarmAttractorBase
{
	public float IdleAttractionScalar = -0.5f;

	public float IdleThrustScalar = 0.0f;

	public void Awake()
	{
		trackedCamera = GameObject.FindObjectOfType<SteamVR_Camera>();
	}

	public override void AppendActiveAttractors(
		ref List<AttractorState> attractors)
	{
		if ((trackedCamera != null) &&
			trackedCamera.isActiveAndEnabled)
		{
			AttractorState attractor = new AttractorState()
			{
				Position = trackedCamera.transform.position,
				Rotation = trackedCamera.transform.rotation,
				AttractionScalar = IdleAttractionScalar,
				ThrustScalar = IdleThrustScalar,
			};

			attractors.Add(attractor);
		}
	}

	private SteamVR_Camera trackedCamera = null;
}
