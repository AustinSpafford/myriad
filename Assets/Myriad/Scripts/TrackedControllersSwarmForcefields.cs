using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedControllersSwarmForcefields : MonoBehaviour
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

	public void OnEnable()
	{
		SwarmForcefieldCollector.CollectingForcefields += OnCollectingForcefields;
	}

	public void OnDisable()
	{
		SwarmForcefieldCollector.CollectingForcefields -= OnCollectingForcefields;
	}

	private void OnCollectingForcefields(
		object sender,
		CollectingForcefieldsEventArgs eventArgs)
	{
		var openVrSystem = OpenVR.System;

		if ((openVrSystem != null) &&
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
						
						eventArgs.ForcefieldAppender.AppendSphericalForcefield(
							trackedObject.transform.position,
							FalloffInnerRadius,
							FalloffOuterRadius,
							(-1.0f * (gripPressed ? GrippedAttractionScalar : IdleAttractionScalar)));
						
						eventArgs.ForcefieldAppender.AppendThrustCapsuleForcefield(
							trackedObject.transform.position,
							(trackedObject.transform.position + trackedObject.transform.forward), // TODO: Make the capsule-length customizable.
							FalloffInnerRadius, // TODO: Give the thrust-capsule its own falloffs.
							FalloffOuterRadius,
							Mathf.Lerp(IdleThrustScalar, TriggerPulledThrustScalar, triggerFraction));
					}
				}
			}
		}
	}

	private SteamVR_ControllerManager controllerManager = null;
}
