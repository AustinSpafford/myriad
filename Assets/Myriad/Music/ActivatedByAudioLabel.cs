using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActivatedByAudioLabel : MonoBehaviour
{
	public GameObject TargetObject = null;

	public bool ActiveDuringAudioLabels = true;

	public List<string> AudioLabelsWhitelist = null;

	public void OnEnable()
	{
		ResetCurrentActivationCount();

		AudioLabelBroadcaster.AudioLabelEventTriggered += OnAudioLabelEvent;
		AudioLabelBroadcaster.AudioLabelStreamRestarting += OnAudioLabelStreamRestarting;
	}

	public void OnDisable()
	{
		AudioLabelBroadcaster.AudioLabelEventTriggered -= OnAudioLabelEvent;
		AudioLabelBroadcaster.AudioLabelStreamRestarting -= OnAudioLabelStreamRestarting;
	}
	
	private int ActiveLabelCount;
	
	private void OnAudioLabelEvent(
		object sender,
		AudioLabelEventArgs eventArgs)
	{
		switch (eventArgs.EventType)
		{
			case AudioLabelEventType.Immediate:
			{
				// There's no sensible response to an immediate-label.
				break;
			}

			case AudioLabelEventType.IntervalStarting:
			{
				if (AudioLabelsWhitelist.Contains(eventArgs.LabelName))
				{
					++ActiveLabelCount;
				}

				UpdateTargetActivation();

				break;
			}

			case AudioLabelEventType.IntervalEnding:
			{
				if (AudioLabelsWhitelist.Contains(eventArgs.LabelName))
				{
					--ActiveLabelCount;
				}

				UpdateTargetActivation();

				break;
			}

			default:
				throw new System.InvalidOperationException();
		}
	}
	
	private void OnAudioLabelStreamRestarting(
		object sender,
		AudioLabelStreamRestartingEventArgs eventArgs)
	{
		ResetCurrentActivationCount();
	}

	private void ResetCurrentActivationCount()
	{
		ActiveLabelCount = 0;

		UpdateTargetActivation();
	}

	private void UpdateTargetActivation()
	{
		bool anyLabelsAreActive = (ActiveLabelCount > 0);

		bool targetShouldBeActive = (ActiveDuringAudioLabels ? anyLabelsAreActive : !anyLabelsAreActive);

		if (targetShouldBeActive != TargetObject.activeSelf)
		{
			TargetObject.SetActive(targetShouldBeActive);
		}
	}
}
