using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FlatscreenRotationControls : MonoBehaviour
{
	public void Update()
	{
		bool shouldBeMouseLooking = Input.GetMouseButton(0);

		if (shouldBeMouseLooking && !isMouseLooking)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			isMouseLooking = true;
		}
		else if (!shouldBeMouseLooking && isMouseLooking)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			isMouseLooking = false;
		}

		if (isMouseLooking)
		{
			Vector2 mouseDelta = 
				new Vector2(
					Input.GetAxisRaw("Mouse X"),
					Input.GetAxisRaw("Mouse Y"));

			// Keeping it simple, we'll just work in terms of the world-oriented euler angles.
			Vector3 eulerAngles = transform.rotation.eulerAngles;

			eulerAngles.y += mouseDelta.x;
			eulerAngles.x += (-1.0f * mouseDelta.y);

			// Clamp the pitch to avoid limit-jitter.
			eulerAngles.x = Mathf.Clamp(
				Mathf.DeltaAngle(0.0f, eulerAngles.x),
				-90.0f,
				90.0f);

			transform.rotation = Quaternion.Euler(eulerAngles);
		}
	}

	private bool isMouseLooking = false;
}