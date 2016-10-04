using UnityEngine;
using System.Collections;

public class Turntable : MonoBehaviour
{
	public Vector3 LocalRotationAxis = Vector3.up;

	public float DegreesPerSecond = 30.0f;

	public void Update()
	{
		transform.localRotation *= 
			Quaternion.AngleAxis(
				(DegreesPerSecond * Time.deltaTime),
				LocalRotationAxis);
	}
}