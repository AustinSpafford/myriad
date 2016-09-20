using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedControllersSwarmForcefields : SwarmForcefieldsBase
{
	public float FalloffInnerRadius = 0.5f;
	public float FalloffOuterRadius = 1.0f;

	public float IdleAttractionScalar = -0.2f;
	public float GrippedAttractionScalar = 1.0f;

	public float IdleThrustScalar = 0.0f;
	public float TriggerPulledThrustScalar = 5.0f;

	public void Awake()
	{
		controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();
	}

	public void Start()
	{
		// An empty Start() forces the inspector to add an Enabled-checkbox.
	}

	public override void AppendActiveForcefields(
		ref List<SwarmShaderForcefieldState> forcefields)
	{
		var openVrSystem = OpenVR.System;

		if (isActiveAndEnabled &&
			(openVrSystem != null) &&
			(controllerManager != null))
		{
			foreach (GameObject controllerObject in controllerManager.objects)
			{
				SteamVR_TrackedObject trackedObject = controllerObject.GetComponent<SteamVR_TrackedObject>();
				
				if (trackedObject.index != SteamVR_TrackedObject.EIndex.None)
				{
					var controllerDevice = SteamVR_Controller.Input((int)trackedObject.index);

					if ((trackedObject != null) &&
						trackedObject.isActiveAndEnabled &&
						controllerDevice.valid)
					{
						bool gripPressed = controllerDevice.GetPress(EVRButtonId.k_EButton_Grip);
					
						float triggerFraction = controllerDevice.GetAxis(EVRButtonId.k_EButton_SteamVR_Trigger).x;
						
						var forcefield = new SwarmShaderForcefieldState()
						{
							Position = trackedObject.transform.position,
							FalloffInnerRadius = this.FalloffInnerRadius,
							FalloffOuterRadius = this.FalloffOuterRadius,
							AttractionScalar = (gripPressed ? GrippedAttractionScalar : IdleAttractionScalar),
							ThrustDirection = trackedObject.transform.forward,
							ThrustScalar = Mathf.Lerp(IdleThrustScalar, TriggerPulledThrustScalar, triggerFraction),
						};

						forcefields.Add(forcefield);
					}
				}
			}
		}
	}

	private SteamVR_ControllerManager controllerManager = null;
}
