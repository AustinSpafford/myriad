using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeactivateChildrenDuringVRLoads : MonoBehaviour
{
	public GameObject[] TargetChildren = new GameObject[0];

	public void OnEnable()
	{
		VRDeviceLoader.IsLoadingVRDeviceChanged += OnIsLoadingVRDeviceChanged;
	}

	public void OnDisable()
	{
		VRDeviceLoader.IsLoadingVRDeviceChanged -= OnIsLoadingVRDeviceChanged;
	}

	private void OnIsLoadingVRDeviceChanged(
		object sender,
		IsLoadingVRDeviceChangedEventArgs eventArgs)
	{
		bool shouldEnableChildren = (eventArgs.IsLoadingVRDevice == false);

		foreach (GameObject targetChild in TargetChildren)
		{
			targetChild.SetActive(shouldEnableChildren);
		}
	}
}
