using UnityEngine;
using System;
using System.Collections;

public class MimicTransform : MonoBehaviour
{
	public Transform CopiedTransform = null;

	public void LateUpdate()
	{
		if (CopiedTransform != null)
		{
			transform.position = CopiedTransform.position;
			transform.rotation = CopiedTransform.rotation;
			transform.localScale = (Vector3.one * (CopiedTransform.lossyScale.magnitude / Mathf.Sqrt(3.0f)));
		}
	}
}
