using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AudioLabelRemapper : MonoBehaviour
{
	[Serializable]
	public struct LabelRemappingEntry
	{
		public string OriginalLabelPrefix;

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
	private List<LabelRemappingEntry> LabelRemappings = new List<LabelRemappingEntry>();

	private void OnAudioLabelRemapping(
		object sender,
		AudioLabelRemappingEventArgs eventArgs)
	{
	}
}
