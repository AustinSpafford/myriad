// The linux build was failing to include VRSettings, hence the workaround.
#if !UNITY_STANDALONE_LINUX
#define VR_SETTINGS_AVAILABLE
#endif

using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using System.Linq;

public class ActivateChildrenViaVREnabled : MonoBehaviour
{
	public GameObject[] ActiveWhenVREnabled = new GameObject[0];
	public GameObject[] ActiveWhenVRDisabled = new GameObject[0];

	public void OnEnable()
	{
		VRDeviceLoader.LoadedVRDeviceChanged += OnLoadedVRDeviceChanged;
		
		UpdateActivationStates();
	}

	public void OnDisable()
	{
		VRDeviceLoader.LoadedVRDeviceChanged -= OnLoadedVRDeviceChanged;
	}

	private void UpdateActivationStates()
	{
#if VR_SETTINGS_AVAILABLE
		bool vrIsEnabled = VRSettings.enabled;
#else
		bool vrIsEnabled = false;
#endif // VR_SETTINGS_AVAILABLE

		foreach (GameObject childActiveWhenVREnabled in ActiveWhenVREnabled)
		{
			childActiveWhenVREnabled.SetActive(vrIsEnabled);
		}

		foreach (GameObject childInactiveWhenVREnabled in ActiveWhenVRDisabled)
		{
			childInactiveWhenVREnabled.SetActive(vrIsEnabled == false);
		}
	}
	
	private void OnLoadedVRDeviceChanged(
		object sender,
		LoadedVRDeviceChangedEventArgs eventArgs)
	{
		UpdateActivationStates();
	}
}