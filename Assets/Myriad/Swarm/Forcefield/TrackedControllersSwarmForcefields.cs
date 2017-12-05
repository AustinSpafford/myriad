using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedControllersSwarmForcefields : MonoBehaviour
{
	public float AttractionFalloffInnerRadius = 0.5f;
	public float AttractionFalloffOuterRadius = 1.0f;

	public float IdleAttractionScalar = -0.2f;
	public float GrippedAttractionScalar = 1.0f;

	public bool UseTriggerAsGripAlias = false;
	
	public float ThrustFalloffInnerRadius = 0.2f;
	public float ThrustFalloffOuterRadius = 0.4f;
	public float ThrustCoreLength = 10.0f;

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
						float localToWorldUniformScale = 
							(trackedObject.transform.localToWorldMatrix.GetScale().magnitude / Mathf.Sqrt(3.0f));

						float gripFraction = (controllerDevice.GetPress(EVRButtonId.k_EButton_Grip) ? 1.0f : 0.0f);
						float triggerFraction = controllerDevice.GetAxis(EVRButtonId.k_EButton_SteamVR_Trigger).x;
						
						if (UseTriggerAsGripAlias)
						{
							gripFraction = Mathf.Clamp01(gripFraction + triggerFraction);
							triggerFraction = 0.0f;
						}

						eventArgs.ForcefieldAppender.AppendSphericalForcefield(
							trackedObject.transform.position,
							(AttractionFalloffInnerRadius * localToWorldUniformScale),
							(AttractionFalloffOuterRadius * localToWorldUniformScale),
							(-1.0f * Mathf.Lerp(IdleAttractionScalar, GrippedAttractionScalar, gripFraction)));
						
						eventArgs.ForcefieldAppender.AppendThrustCapsuleForcefield(
							trackedObject.transform.position,
							(trackedObject.transform.position + (Mathf.Max(0.0001f, ThrustCoreLength) * trackedObject.transform.forward)),
							(ThrustFalloffInnerRadius * localToWorldUniformScale),
							(ThrustFalloffOuterRadius * localToWorldUniformScale),
							Mathf.Lerp(IdleThrustScalar, TriggerPulledThrustScalar, triggerFraction));
					}
				}
			}
		}
	}

	private SteamVR_ControllerManager controllerManager = null;
}
