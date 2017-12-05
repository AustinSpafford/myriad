// The linux build was failing to include VRSettings, hence the workaround.
#if !UNITY_STANDALONE_LINUX
#define VR_SETTINGS_AVAILABLE
#endif

using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LoadedVRDeviceChangedEventArgs : System.EventArgs
{
	public string LoadedDeviceName;
	public bool VRIsEnabled;
}

public class IsLoadingVRDeviceChangedEventArgs : System.EventArgs
{
	public bool IsLoadingVRDevice;
}

public class VRDeviceLoader : MonoBehaviour
{
	public bool StartupVRLoadingEnabled = true;

	public static event System.EventHandler<LoadedVRDeviceChangedEventArgs> LoadedVRDeviceChanged;
	public static event System.EventHandler<IsLoadingVRDeviceChangedEventArgs> IsLoadingVRDeviceChanged;

	public static bool IsLoadingVRDevice { get; private set; }

	public void Start()
	{
		StartCoroutine(LoadVRDeviceAsync());
	}

	private IEnumerator LoadVRDeviceAsync()
	{
#if VR_SETTINGS_AVAILABLE
		SetIsLoadingVRDevice(true);

		string targetDeviceName = 
			(StartupVRLoadingEnabled ? "OpenVR" : "None");

		if (UnityEngine.XR.XRSettings.loadedDeviceName != targetDeviceName)
		{
			UnityEngine.XR.XRSettings.LoadDeviceByName(targetDeviceName);

			// Delay for a single frame to permit the load-attempt.
			yield return null;

			// If we successfully loaded a VR device.
			if (string.IsNullOrEmpty(UnityEngine.XR.XRSettings.loadedDeviceName) == false)
			{
				UnityEngine.XR.XRSettings.enabled = true;
			
				if (LoadedVRDeviceChanged != null)
				{
					var eventArgs = new LoadedVRDeviceChangedEventArgs();
					eventArgs.LoadedDeviceName = UnityEngine.XR.XRSettings.loadedDeviceName;
					eventArgs.VRIsEnabled = UnityEngine.XR.XRSettings.enabled;

					LoadedVRDeviceChanged(this, eventArgs);
				}
			}
			else
			{
				// We failed to initialize, so fall back to flatscreen-mode.
				UnityEngine.XR.XRSettings.LoadDeviceByName("None");
			}
		}
		       
		SetIsLoadingVRDevice(false);
#else
		yield return null; // Stubbed return to keep C# appeased.
#endif // VR_SETTINGS_AVAILABLE
	}

	private void SetIsLoadingVRDevice(
		bool isLoadingVRDevice)
	{		
		IsLoadingVRDevice = isLoadingVRDevice;

		if (IsLoadingVRDeviceChanged != null)
		{
			var eventArgs = new IsLoadingVRDeviceChangedEventArgs();
			eventArgs.IsLoadingVRDevice = IsLoadingVRDevice;

			IsLoadingVRDeviceChanged(this, eventArgs);
		}
	}
}