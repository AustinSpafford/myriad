using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioLabelTracker : MonoBehaviour
{
	public TextAsset LabelsFile = null;

	public void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void Update()
	{
		if (LabelsFile != parsedLabelsFile)
		{
			// Release the existing labels.
			if (parsedLabelsFile != null)
			{
				parsedLabelsFile = null;
				
				// TODO Release.
			}

			// Parse the new labels.
			if (LabelsFile != null)
			{
				// TODO Parse.
			}
		}

		// TODO Reset playback if the target-time is behind our exposed labels.
		// NOTE: Broadcast a rewind-warning so other components know to ditch their state.

		// TODO Seek forward to the target-time, broadcasting all encountered label-events.
	}

	private AudioSource audioSource = null;  

	private TextAsset parsedLabelsFile = null;
}
