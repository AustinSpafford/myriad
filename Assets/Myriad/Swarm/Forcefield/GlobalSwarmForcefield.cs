using UnityEngine;
using System.Collections;

public class GlobalSwarmForcefield : MonoBehaviour
{
	public Vector3 ForceDirection = Vector3.down;

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
		eventArgs.ForcefieldAppender.AppendGlobalForcefield(
			ForceDirection,
			IdleForceScalar);
	}
}
