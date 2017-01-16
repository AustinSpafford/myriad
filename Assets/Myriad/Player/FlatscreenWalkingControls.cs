using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FlatscreenWalkingControls : MonoBehaviour
{
	public float Speed = 1.0f;

	public void Update()
	{
		Vector3 naiveInputDirection = 
			new Vector3(
				Input.GetAxis("Horizontal"),
				0.0f,
				Input.GetAxis("Vertical"));

		// Keep people from walking extra-fast on the diagonals.
		if (naiveInputDirection.sqrMagnitude > 1.0f)
		{
			naiveInputDirection = naiveInputDirection.normalized;
		}

		Vector3 cameraOrientedInputDirection = (
			Quaternion.AngleAxis(
				Camera.main.transform.rotation.eulerAngles.y,
				Vector3.up) *
			naiveInputDirection);

		transform.localPosition += (cameraOrientedInputDirection * (Speed * Time.deltaTime));
	}
}