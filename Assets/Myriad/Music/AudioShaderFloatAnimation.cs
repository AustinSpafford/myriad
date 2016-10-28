using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AudioShaderFloatAnimation : MonoBehaviour
{
	public string ShaderUniformName;

	[System.Serializable]
	public class AudioFloatEvent
	{
		public string AudioLabelName;

		public float LabelStartTargetValue;
		public float LabelStartBlendTime;

		public float LabelEndTargetValue;
		public float LabelEndBlendTime;
	}

	public bool DebugEnabled = false;

	public void OnStart()
	{
		ResetShaderUniform();
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

	public void Update()
	{
		BlendFraction = 
			Mathf.SmoothDamp(
				BlendFraction,
				1.0f,
				ref BlendVelocity,
				BlendTime);
	}
	
	private void OnAudioLabelEvent(
		object sender,
		AudioLabelEventArgs eventArgs)
	{
		foreach (AudioFloatEvent colorEvent in audioFloatEvents)
		{
			if (colorEvent.AudioLabelName == eventArgs.LabelName)
			{
				switch (eventArgs.EventType)
				{
					case AudioLabelEventType.Immediate:
						BlendStartUniformValue = colorEvent.LabelStartTargetValue;
						BlendEndUniformValue = colorEvent.LabelEndTargetValue;
						BlendTime = colorEvent.LabelEndBlendTime;
						BlendFraction = 0.0f;
						BlendVelocity = 0.0f;
						break;
						
					case AudioLabelEventType.IntervalStarting:
						BlendStartUniformValue = GetCurrentUniformValue();
						BlendEndUniformValue = colorEvent.LabelStartTargetValue;
						BlendTime = colorEvent.LabelStartBlendTime;
						BlendFraction = 0.0f;
						BlendVelocity = 0.0f;
						break;
						
					case AudioLabelEventType.IntervalEnding:
						BlendStartUniformValue = GetCurrentUniformValue();
						BlendEndUniformValue = colorEvent.LabelEndTargetValue;
						BlendTime = colorEvent.LabelEndBlendTime;
						BlendFraction = 0.0f;
						BlendVelocity = 0.0f;
						break;

					default:
						throw new System.ComponentModel.InvalidEnumArgumentException();
				}

				if (DebugEnabled)
				{
					Debug.LogFormat(
						"[{0}] blending from [{1}] to [{2}] over [{3}]",
						ShaderUniformName,
						BlendStartUniformValue,
						BlendEndUniformValue,
						BlendTime);
				}
			}
		}
	}
	
	private void OnAudioLabelStreamRestarting(
		object sender,
		AudioLabelStreamRestartingEventArgs eventArgs)
	{
		ResetShaderUniform();
	}
	
	private void OnCollectingShaderUniforms(
		object sender,
		CollectingAudioShaderUniformsEventArgs eventArgs)
	{
		if (ShaderUniformName != null)
		{
			eventArgs.UniformAccessor.SetFloat(
				ShaderUniformName,
				GetCurrentUniformValue());
		}
	}

	[SerializeField]
	private List<AudioFloatEvent> audioFloatEvents = new List<AudioFloatEvent>();	

	private float BlendStartUniformValue;
	private float BlendEndUniformValue;

	private float BlendTime;
	private float BlendFraction;
	private float BlendVelocity;

	private float GetCurrentUniformValue()
	{
		return Mathf.Lerp(BlendStartUniformValue, BlendEndUniformValue, BlendFraction);
	}

	private void ResetShaderUniform()
	{
		AudioFloatEvent lastFloatEvent = audioFloatEvents.LastOrDefault();

		BlendStartUniformValue = lastFloatEvent.LabelStartTargetValue;
		BlendEndUniformValue = lastFloatEvent.LabelEndTargetValue;

		BlendTime = lastFloatEvent.LabelEndBlendTime;
		BlendFraction = 1.0f;
		BlendVelocity = 0.0f;
	}
}
