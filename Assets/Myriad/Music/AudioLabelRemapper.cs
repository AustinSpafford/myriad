using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AudioLabelRemapper : MonoBehaviour
{
	[Serializable]
	public struct LabelRemappingEntry
	{
		public string OriginalLabelTrack;

		public bool OriginalLabelRangeIsEnabled;
		public int OriginalLabelRangeFirst;
		public int OriginalLabelRangeLast;

		public string RemappedLabelName;
	}

	public void OnEnable()
	{
		AudioLabelBroadcaster.AudioLabelRemapping += OnAudioLabelRemapping;
	}

	public void OnDisable()
	{
		AudioLabelBroadcaster.AudioLabelRemapping -= OnAudioLabelRemapping;
	}

	[SerializeField]
	private List<LabelRemappingEntry> labelRemappings = new List<LabelRemappingEntry>();

	private void OnAudioLabelRemapping(
		object sender,
		AudioLabelRemappingEventArgs eventArgs)
	{
		string originalLabelTrack = new string(eventArgs.OriginalLabelName.Where(Char.IsLetter).ToArray());

		int originalLabelNumberValue;
		bool originalLabelHasNumber =
			int.TryParse(
				eventArgs.OriginalLabelName.Substring(originalLabelTrack.Length),
				out originalLabelNumberValue);

		foreach (LabelRemappingEntry remappingEntry in labelRemappings)
		{
			if (originalLabelTrack.Equals(remappingEntry.OriginalLabelTrack, StringComparison.OrdinalIgnoreCase))
			{
				bool numberRangeIsMatched = (
					(remappingEntry.OriginalLabelRangeIsEnabled == false) ||
					(
						originalLabelHasNumber &&
						(remappingEntry.OriginalLabelRangeFirst <= originalLabelNumberValue) &&
						(originalLabelNumberValue <= remappingEntry.OriginalLabelRangeLast)
					));
				
				if (numberRangeIsMatched)
				{
					eventArgs.OutRemappedLabelNames.Add(remappingEntry.RemappedLabelName);
				}
			}
		}
	}
}
