using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedControllersSwarmAttractor : SwarmAttractorBase
{
	public float IdleAttractionScalar = 1.0f;
	public float GrippedAttractionScalar = -1.0f;

	public float IdleThrustScalar = 0.0f;
	public float TriggerPulledThrustScalar = 1.0f;

	public void Awake()
	{
		controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();
	}

	public override void AppendActiveAttractors(
		ref List<AttractorState> attractors)
	{
		var openVrSystem = OpenVR.System;

		if ((openVrSystem != null) &&
			(controllerManager != null))
		{
			foreach (GameObject controllerObject in controllerManager.objects)
			{
				SteamVR_TrackedObject trackedObject = controllerObject.GetComponent<SteamVR_TrackedObject>();
				
				VRControllerState_t controllerState = new VRControllerState_t();

				if ((trackedObject != null) &&
					trackedObject.isActiveAndEnabled &&
					openVrSystem.GetControllerState((uint)trackedObject.index, ref controllerState))
				{
					bool gripPressed = 
						((controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_Grip))) != 0);
					
					bool triggerPressed = 
						((controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_SteamVR_Trigger))) != 0);

					AttractorState attractor = new AttractorState()
					{
						Position = trackedObject.transform.position,
						Rotation = trackedObject.transform.rotation,
						AttractionScalar = (gripPressed ? -1.0f : 1.0f),
						ThrustScalar = (triggerPressed ? 1.0f : 0.0f),
					};

					attractors.Add(attractor);
				}
			}
		}
	}

	private SteamVR_ControllerManager controllerManager = null;
}
