using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class TrackedCamerasSwarmForcefields : MonoBehaviour
{
	public float FalloffInnerRadius = 0.2f;
	public float FalloffOuterRadius = 0.4f;

	public float IdleAttractionScalar = -5.0f;

	public float IdleThrustScalar = 0.0f;

	public void Awake()
	{
		trackedCamera = GameObject.FindObjectOfType<SteamVR_Camera>();
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
		if ((trackedCamera != null) &&
			trackedCamera.isActiveAndEnabled)
		{
			float localToWorldUniformScale = 
				(trackedCamera.transform.localToWorldMatrix.GetScale().magnitude / Mathf.Sqrt(3.0f));

			eventArgs.ForcefieldAppender.AppendSphericalForcefield(
				trackedCamera.transform.position,
				(FalloffInnerRadius * localToWorldUniformScale),
				(FalloffOuterRadius * localToWorldUniformScale),
				(-1.0f * IdleAttractionScalar));
		}
	}

	private SteamVR_Camera trackedCamera = null;
}
