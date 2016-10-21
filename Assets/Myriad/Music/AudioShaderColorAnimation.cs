using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioShaderColorAnimation : MonoBehaviour
{
	[System.Serializable]
	public class AudioColorEvent
	{
		public string ShaderUniformName;
		
		public string AudioLabelName;

		public Color LabelStartColor;
		public float LabelStartBlendTime;

		public Color LabelEndColor;
		public float LabelEndBlendTime;
	}

	public void OnStart()
	{
		ResetColorStates();
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
		foreach (AudioColorState colorState in audioColorStates.Values)
		{
			colorState.BlendFraction = 
				Mathf.SmoothDamp(
					colorState.BlendFraction,
					1.0f,
					ref colorState.BlendVelocity,
					colorState.BlendTime);
		}
	}
	
	private void OnAudioLabelEvent(
		object sender,
		AudioLabelEventArgs eventArgs)
	{
		foreach (AudioColorEvent colorEvent in audioColorEvents)
		{
			if (colorEvent.AudioLabelName == eventArgs.LabelName)
			{
				AudioColorState colorState = GetOrCreateAudioColorState(colorEvent.ShaderUniformName);

				switch (eventArgs.EventType)
				{
					case AudioLabelEventType.Immediate:
						colorState.StartColor = colorEvent.LabelStartColor;
						colorState.EndColor = colorEvent.LabelEndColor;
						colorState.BlendTime = colorEvent.LabelEndBlendTime;
						colorState.BlendFraction = 0.0f;
						colorState.BlendVelocity = 0.0f;
						break;
						
					case AudioLabelEventType.IntervalStarting:
						colorState.StartColor = GetCurrentAudioColor(colorState);
						colorState.EndColor = colorEvent.LabelStartColor;
						colorState.BlendTime = colorEvent.LabelStartBlendTime;
						colorState.BlendFraction = 0.0f;
						colorState.BlendVelocity = 0.0f;
						break;
						
					case AudioLabelEventType.IntervalEnding:
						colorState.StartColor = GetCurrentAudioColor(colorState);
						colorState.EndColor = colorEvent.LabelEndColor;
						colorState.BlendTime = colorEvent.LabelEndBlendTime;
						colorState.BlendFraction = 0.0f;
						colorState.BlendVelocity = 0.0f;
						break;

					default:
						throw new System.ComponentModel.InvalidEnumArgumentException();
				}
			}
		}
	}
	
	private void OnAudioLabelStreamRestarting(
		object sender,
		AudioLabelStreamRestartingEventArgs eventArgs)
	{
		ResetColorStates();
	}
	
	private void OnCollectingShaderUniforms(
		object sender,
		CollectingAudioShaderUniformsEventArgs eventArgs)
	{
		foreach (AudioColorState colorState in audioColorStates.Values)
		{
			eventArgs.UniformAccessor.SetColor(
				colorState.ShaderUniformName,
				GetCurrentAudioColor(colorState));
		}
	}
	
	private class AudioColorState
	{
		public string ShaderUniformName;

		public Color StartColor;
		public Color EndColor;

		public float BlendTime;
		public float BlendFraction;
		public float BlendVelocity;
	}

	[SerializeField]
	private List<AudioColorEvent> audioColorEvents = new List<AudioColorEvent>();
	
	private Dictionary<string, AudioColorState> audioColorStates = new Dictionary<string, AudioColorState>();	

	private Color GetCurrentAudioColor(
		AudioColorState colorState)
	{
		return LogrithmicColorLerp(
			colorState.StartColor, 
			colorState.EndColor, 
			colorState.BlendFraction);
	}

	private AudioColorState GetOrCreateAudioColorState(
		string shaderUniformName)
	{
		AudioColorState result = null;

		if (audioColorStates.TryGetValue(shaderUniformName, out result) == false)
		{
			result = new AudioColorState()
			{
				ShaderUniformName = shaderUniformName,
			};

			audioColorStates.Add(shaderUniformName, result);
		}

		return result;
	}

	private Color LogrithmicColorLerp(
		Color startColor,
		Color endColor,
		float lerpFraction)
	{
		Color startEnergy = (startColor * startColor);
		Color endEnergy = (endColor * endColor);

		// NOTE: We're intentionally not using Color.Lerp(), because we don't know whether it's simple or energy-based.
		Color resultEnergy = (
			startEnergy + 
			Mathf.Clamp01(lerpFraction) * (endEnergy - startEnergy));

		Color result = new Color(
			Mathf.Sqrt(resultEnergy.r),
			Mathf.Sqrt(resultEnergy.g),
			Mathf.Sqrt(resultEnergy.b));

		return result;
	}

	private void ResetColorStates()
	{
		foreach (AudioColorEvent colorEvent in audioColorEvents)
		{
			AudioColorState colorState = GetOrCreateAudioColorState(colorEvent.ShaderUniformName);

			colorState.StartColor = colorEvent.LabelStartColor;
			colorState.EndColor = colorEvent.LabelEndColor;
			colorState.BlendTime = colorEvent.LabelEndBlendTime;

			colorState.BlendFraction = 1.0f;
			colorState.BlendVelocity = 0.0f;
		} 
	}
}
