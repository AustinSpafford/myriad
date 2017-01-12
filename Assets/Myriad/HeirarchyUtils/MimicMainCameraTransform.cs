using UnityEngine;
using System;
using System.Collections;

public class MimicMainCameraTransform : MonoBehaviour
{
	public void LateUpdate()
	{
		Camera mainCamera = Camera.main;

		if (mainCamera != null)
		{
			transform.position = mainCamera.transform.position;
			transform.rotation = mainCamera.transform.rotation;
			transform.localScale = (Vector3.one * (mainCamera.transform.lossyScale.magnitude / Mathf.Sqrt(3.0f)));
		}
	}
}

