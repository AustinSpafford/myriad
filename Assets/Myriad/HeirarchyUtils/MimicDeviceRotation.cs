using UnityEngine;
using System.Collections;

public class MimicDeviceRotation : MonoBehaviour
{
	public bool TrackingEnabled = true;

	public void Start()
	{
		Input.compass.enabled = true;
		Input.gyro.enabled = true;
	}

	public void FixedUpdate()
	{
		if (Input.GetMouseButtonDown(0))
		{
			TrackingEnabled = !TrackingEnabled;
		}

		if (TrackingEnabled)
		{
			// The device's coordinate system is right-handed, such that we must flip the z-axis.
			// Note that he w-coordinate has to also be flipped to retain the direction of rotation.
			// Unfortunately I'm failing to find an appropriate math-resource to clearly describe *why* this works.
			Quaternion unitySpaceDeviceRotation = new Quaternion(
				Input.gyro.attitude.x,
				Input.gyro.attitude.y,
				(-1 * Input.gyro.attitude.z),
				(-1 * Input.gyro.attitude.w));
		
			transform.localRotation = (
				Quaternion.AngleAxis(90.0f, Vector3.right) * // Rotate from looking at the ground to looking forward.
				unitySpaceDeviceRotation);
		}
	}
}