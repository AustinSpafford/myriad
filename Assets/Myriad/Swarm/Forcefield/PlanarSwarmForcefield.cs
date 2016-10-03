using UnityEngine;
using System.Collections;

public class PlanarSwarmForcefield : MonoBehaviour
{
	public Vector3 LocalPosition = Vector3.zero;
	public Vector3 LocalPlaneNormal = Vector3.up;

	public float FalloffInnerRadius = 0.5f;
	public float FalloffOuterRadius = 1.0f;

	public float IdleForceScalar = 1.0f;

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
		float localToWorldUniformScale = 
			(transform.localToWorldMatrix.GetScale().magnitude / Mathf.Sqrt(3.0f));

		eventArgs.ForcefieldAppender.AppendPlanarForcefield(
			transform.TransformPoint(LocalPosition),
			transform.TransformDirection(LocalPlaneNormal),
			(FalloffInnerRadius * localToWorldUniformScale),
			(FalloffOuterRadius * localToWorldUniformScale),
			IdleForceScalar);
	}
}
