using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class AudioLabelBroadcaster : MonoBehaviour
{
	public TextAsset LabelsFile = null;

	public void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void Update()
	{
		if (LabelsFile != loadedLabelsFile)
		{
			LoadLabelsFile(LabelsFile);
		}

		// TODO Reset playback if the target-time is behind our exposed labels.
		// NOTE: Broadcast a rewind-warning so other components know to ditch their state.

		// TODO Seek forward to the target-time, broadcasting all encountered label-events.
	}

	private enum LabelEventType
	{
		ImmediateLabel,
		DurationLabelStarting,
		DurationLabelEnding,
	}

	private struct AudioLabelEvent
	{
		public LabelEventType EventType;
		public string LabelName;
		public float EventTime;
	}

	private AudioSource audioSource = null;  

	private TextAsset loadedLabelsFile = null;
	private List<AudioLabelEvent> sortedLabelEvents = new List<AudioLabelEvent>();

	private void LoadLabelsFile(
		TextAsset newLabelsFile)
	{
		// Release the existing labels.
		if (loadedLabelsFile != null)
		{
			sortedLabelEvents.Clear();

			loadedLabelsFile = null;
		}

		// Parse the new labels.
		if (newLabelsFile != null)
		{
			using (var fileReader = new StringReader(newLabelsFile.text))
			{
				string line;
				while ((line = fileReader.ReadLine()) != null)
				{
					string[] tokens = line.Split(null);

					if (tokens.Length >= 3)
					{
						float startTime;
						float endTime;
						if (float.TryParse(tokens[0], out startTime) &&
							float.TryParse(tokens[1], out endTime))
						{
							string labelName = tokens[2];

							if (startTime == endTime)
							{
								sortedLabelEvents.Add(new AudioLabelEvent()
								{
									EventType = LabelEventType.ImmediateLabel,
									LabelName = labelName,
									EventTime = startTime,
								});
							}
							else
							{
								sortedLabelEvents.Add(new AudioLabelEvent()
								{
									EventType = LabelEventType.DurationLabelStarting,
									LabelName = labelName,
									EventTime = startTime,
								});

								sortedLabelEvents.Add(new AudioLabelEvent()
								{
									EventType = LabelEventType.DurationLabelEnding,
									LabelName = labelName,
									EventTime = endTime,
								});
							}
						}
					}
				}
			}

			// Chronologically sort the events.
			// Note that we can't use the standard in-place Sort() function, because it's unstable. 
			// However, OrderBy() is explicitly documented as being a stable sort, so it's fair-game.
			sortedLabelEvents = new List<AudioLabelEvent>(sortedLabelEvents.OrderBy(elem => elem.EventTime));

			loadedLabelsFile = newLabelsFile;
		}
	}
}
