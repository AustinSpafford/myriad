using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum AudioLabelEventType
{
	ImmediateLabel,
	IntervalLabelStarting,
	IntervalLabelEnding,
}

public class AudioLabelEventArgs : EventArgs
{
	public AudioLabelBroadcaster Broadcaster;
	public bool BroadcasterIsSeeking; // True when events are being rapidly sent out while seeking the play-head forward over a long distance/time.
	
	public AudioSource CorrespondingAudio;
	
	public float EventTime;
	public AudioLabelEventType EventType;
	public string LabelName;
}

public class AudioLabelEventStreamRestartingEventArgs : EventArgs
{
	public AudioLabelBroadcaster Broadcaster;
}

[RequireComponent(typeof(AudioSource))]
public class AudioLabelBroadcaster : MonoBehaviour
{
	public TextAsset LabelsFile = null;

	public string[] IgnoredLabelPrefixes = new string[0];

	public float MaxLoadMillisecondsPerFrame = 1.0f;

	public bool DebugEnabled = false;

	public bool IsReady { get { return (loadedLabelsFile != null); } }

	// The audio-label event is the main purpose for this class.
	// It allows annotation-files to be spooled out to other systems as our sibling audio-source is played.
	public static event EventHandler<AudioLabelEventArgs> AudioLabelEventTriggered;

	// The stream-restarting event is sent out whenever the audio-clip's playhead moves 
	// backwards in time, forcing us to throw away everything and re-seek to its new position.
	public static event EventHandler<AudioLabelEventStreamRestartingEventArgs> AudioLabelEventStreamRestarting;

	public void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void Update()
	{
		// If we need to load a different labels file.
		if (LabelsFile != loadedLabelsFile)
		{
			// Immediately release the existing labels, since we know it's not what we want.
			if (loadedLabelsFile != null)
			{
				sortedLabelEvents = null;
				nextLabelEventIndex = 0;

				loadedLabelsFile = null;
			}

			// If we need to kick off the file-load.
			if ((LabelsFile != null) &&
				(asyncLoadCoroutine == null))
			{
				asyncLoadCoroutine = StartCoroutine(LoadLabelsFileAsync(LabelsFile));
			}
		}
		
		if (loadedLabelsFile != null)
		{
			// If the playhead moved backwards in time such that our last-broadcast event is no longer appropriate.
			if ((nextLabelEventIndex > 0) &&
				(audioSource.time < sortedLabelEvents[nextLabelEventIndex - 1].EventTime))
			{
				if (DebugEnabled)
				{
					Debug.Log("Restarting event stream!");
				}

				// Notify that we're restarting the event-stream.
				{
					var eventArgs = new AudioLabelEventStreamRestartingEventArgs()
					{
						Broadcaster = this,
					};

					if (AudioLabelEventStreamRestarting != null)
					{
						AudioLabelEventStreamRestarting(this, eventArgs);
					}
				}

				// Re-seek to the current time, but flagging the events as being seek-spam so
				// consumers can choose to opt-out of any expensive operations.
				{
					nextLabelEventIndex = 0;

					BroadcastLabelEvents(
						audioSource.time, 
						broadcasterIsSeeking: true);
				}
			}
		
			BroadcastLabelEvents(
				audioSource.time, 
				broadcasterIsSeeking: false);
		}
	}

	private struct SortableLabelEvent
	{
		public float EventTime;
		public AudioLabelEventType EventType;
		public string LabelName;
	}

	private AudioSource audioSource = null;  
	
	private Coroutine asyncLoadCoroutine = null;

	private TextAsset loadedLabelsFile = null;
	private List<SortableLabelEvent> sortedLabelEvents = null;

	private int nextLabelEventIndex = 0;
	
	private void BroadcastLabelEvents(
		float targetTime,
		bool broadcasterIsSeeking)
	{
		while ((nextLabelEventIndex < sortedLabelEvents.Count) && 
			(sortedLabelEvents[nextLabelEventIndex].EventTime <= targetTime))
		{
			var sourceLabelEvent = sortedLabelEvents[nextLabelEventIndex];

			var eventArgs = new AudioLabelEventArgs()
			{
				Broadcaster = this,
				BroadcasterIsSeeking = broadcasterIsSeeking,

				CorrespondingAudio = audioSource,

				EventTime = sourceLabelEvent.EventTime,
				EventType = sourceLabelEvent.EventType,
				LabelName = sourceLabelEvent.LabelName,
			};
			
			if (AudioLabelEventTriggered != null)
			{
				AudioLabelEventTriggered(this, eventArgs);
			}

			if (DebugEnabled)
			{
				Debug.LogFormat(
					"{0} {1:n2} {2} {3}", 
					nextLabelEventIndex,
					eventArgs.EventTime,
					eventArgs.EventType,
					eventArgs.LabelName);
			}

			++nextLabelEventIndex;
		}
	}

	private IEnumerator LoadLabelsFileAsync(
		TextAsset newLabelsFile)
	{
		if (DebugEnabled)
		{
			Debug.LogFormat(
				"Starting async load of [{0}].",
				newLabelsFile.name);
		}

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		var inProgressUnsortedLabelEvents = new List<SortableLabelEvent>(500);

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

						bool labelIsIgnored = 
							IgnoredLabelPrefixes
								.Where(ignoredPrefix => labelName.StartsWith(ignoredPrefix))
								.Any();

						if (labelIsIgnored == false)
						{
							if (startTime == endTime)
							{
								inProgressUnsortedLabelEvents.Add(new SortableLabelEvent()
								{
									EventTime = startTime,
									EventType = AudioLabelEventType.ImmediateLabel,
									LabelName = labelName,
								});
							}
							else
							{
								// NOTE: We break the interval-labels into two separate events so they can
								// be sorted chronologically (otherwise overlapping intervals are ambiguous).

								inProgressUnsortedLabelEvents.Add(new SortableLabelEvent()
								{
									EventTime = startTime,
									EventType = AudioLabelEventType.IntervalLabelStarting,
									LabelName = labelName,
								});

								inProgressUnsortedLabelEvents.Add(new SortableLabelEvent()
								{
									EventTime = endTime,
									EventType = AudioLabelEventType.IntervalLabelEnding,
									LabelName = labelName,
								});
							}
						}

						if (stopwatch.Elapsed.TotalMilliseconds >= MaxLoadMillisecondsPerFrame)
						{
							// Sleep the loading operation until the next frame.
							yield return null;

							// Start the time limit for our next loading-interval.
							stopwatch.Reset();
							stopwatch.Start();
						}
					}
				}
			}
		}

		// Finish the loading-operation.
		{
			// Chronologically sort the events.
			// Note that we can't use the standard in-place Sort() function, because it's unstable. 
			// However, OrderBy() is explicitly documented as being a stable sort, so it's fair-game.
			sortedLabelEvents = 
				new List<SortableLabelEvent>(
					inProgressUnsortedLabelEvents.OrderBy(elem => elem.EventTime));

			nextLabelEventIndex = 0;

			loadedLabelsFile = newLabelsFile;

			// NOTE: There's no need to StopCoroutine() because we're about to naturally terminate.
			asyncLoadCoroutine = null;
		}
		
		if (DebugEnabled)
		{
			Debug.LogFormat(
				"Completed async load of [{0}].",
				loadedLabelsFile.name);
		}
	}
}
