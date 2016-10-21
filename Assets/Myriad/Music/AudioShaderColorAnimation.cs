using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioShaderColorAnimation : MonoBehaviour
{
	[System.Serializable]
	public struct AudioColorEvent
	{
		public string ShaderUniformName;
		
		public string AudioLabelName;

		public Color LabelStartColor;
		public float LabelStartBlendTime;

		public Color LabelEndColor;
		public float LabelEndBlendTime;
	}

	public void OnEnable()
	{
		AudioLabelBroadcaster.AudioLabelEventTriggered += OnAudioLabelEvent;
		AudioLabelBroadcaster.AudioLabelStreamRestarting += OnAudioLabelStreamRestarting;
		AudioShaderUniformCollector.CollectingShaderUniforms += OnCollectingShaderUniforms;
	}

	public void OnDisable()
	{
		AudioLabelBroadcaster.AudioLabelEventTriggered -= OnAudioLabelEvent;
		AudioLabelBroadcaster.AudioLabelStreamRestarting -= OnAudioLabelStreamRestarting;
		AudioShaderUniformCollector.CollectingShaderUniforms -= OnCollectingShaderUniforms;
	}
	
	private void OnAudioLabelEvent(
		object sender,
		AudioLabelEventArgs eventArgs)
	{
	}
	
	private void OnAudioLabelStreamRestarting(
		object sender,
		AudioLabelStreamRestartingEventArgs eventArgs)
	{
	}	
	
	private void OnCollectingShaderUniforms(
		object sender,
		CollectingAudioShaderUniformsEventArgs eventArgs)
	{
	}

	[SerializeField]
	private List<AudioColorEvent> audioColorEvents = new List<AudioColorEvent>();
}
