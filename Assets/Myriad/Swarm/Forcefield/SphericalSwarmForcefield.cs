using UnityEngine;
using System.Collections;

public class SphericalSwarmForcefield : MonoBehaviour
{
	public float FalloffInnerRadius = 0.5f;
	public float FalloffOuterRadius = 1.0f;

	public float IdleOutwardForceScalar = 1.0f;

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

		eventArgs.ForcefieldAppender.AppendSphericalForcefield(
			transform.position,
			(FalloffInnerRadius * localToWorldUniformScale),
			(FalloffOuterRadius * localToWorldUniformScale),
			IdleOutwardForceScalar);
	}
}
