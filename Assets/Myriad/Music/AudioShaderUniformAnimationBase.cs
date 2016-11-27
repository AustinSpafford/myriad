using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

abstract public class AudioShaderColorAnimationBase : MonoBehaviour
{
	[System.Serializable]
	public class LabelToValueTargetMapping
	{
		public List<string> AudioLabelNameWhitelist;

		public string DuringLabelValueTargetName;
		public string AfterLabelValueTargetName;
	}
	
	public string ShaderUniformName;

	public List<LabelToValueTargetMapping> labelToValueTargetMappings = new List<LabelToValueTargetMapping>();

	public bool DebugEnabled = false;

	public void Start()
	{
		ResetUniformState();
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
	
	abstract protected void CollectShaderUniformValue(IAudioShaderUniformAccessor uniformAccessor);
	abstract protected void SetUniformValueTarget(string valueTargetName);
	abstract protected void SnapCurrentValueToTarget();
	
	private void OnAudioLabelEvent(
		object sender,
		AudioLabelEventArgs eventArgs)
	{
		switch (eventArgs.EventType)
		{
			case AudioLabelEventType.Immediate:
			{
				TryTriggerValueTargetMapping(
					eventArgs.LabelName,
					(elem => elem.DuringLabelValueTargetName),
					eventArgs.EventType);
				
				SnapCurrentValueToTarget();
					
				TryTriggerValueTargetMapping(
					eventArgs.LabelName,
					(elem => elem.AfterLabelValueTargetName),
					eventArgs.EventType);

				break;
			}

			case AudioLabelEventType.IntervalStarting:
				TryTriggerValueTargetMapping(
					eventArgs.LabelName,
					(elem => elem.DuringLabelValueTargetName),
					eventArgs.EventType);
				break;				

			case AudioLabelEventType.IntervalEnding:
				TryTriggerValueTargetMapping(
					eventArgs.LabelName,
					(elem => elem.AfterLabelValueTargetName),
					eventArgs.EventType);
				break;

			default:
				throw new System.InvalidOperationException();
		}
	}

	private bool TryTriggerValueTargetMapping(
		string audioLabelName,
		System.Func<LabelToValueTargetMapping, string> valueTargetNameExtractionFunc,
		AudioLabelEventType debugSourceEventType)
	{
		bool result = false;
		
		var triggeredMappings = 
			labelToValueTargetMappings
				.Where(elem => elem.AudioLabelNameWhitelist.Contains(audioLabelName))
				.Where(elem => (string.IsNullOrEmpty(valueTargetNameExtractionFunc(elem)) == false));

		if (triggeredMappings.Count() > 1)
		{
			if (DebugEnabled)
			{
				Debug.LogErrorFormat(
					"There are [{0}] mappings for [{1}]+[{2}]! Aborting due to ambiguity.",
					triggeredMappings.Count(),
					audioLabelName,
					debugSourceEventType);
			}
		}
		else
		{
			LabelToValueTargetMapping mapping = triggeredMappings.SingleOrDefault();

			if (mapping != null)
			{
				string valueTargetName = valueTargetNameExtractionFunc(mapping);

				if (DebugEnabled)
				{
					Debug.LogFormat("Setting [{0}] to [{1}],", ShaderUniformName, valueTargetName);
				}

				SetUniformValueTarget(valueTargetName);

				result = true;
			}
		}

		return result;
	}
	
	private void OnAudioLabelStreamRestarting(
		object sender,
		AudioLabelStreamRestartingEventArgs eventArgs)
	{
		ResetUniformState();
	}
	
	private void OnCollectingShaderUniforms(
		object sender,
		CollectingAudioShaderUniformsEventArgs eventArgs)
	{
		CollectShaderUniformValue(eventArgs.UniformAccessor);
	}
	
	private void ResetUniformState()
	{
		if (labelToValueTargetMappings.Any())
		{
			SetUniformValueTarget(labelToValueTargetMappings.Last().AfterLabelValueTargetName);

			SnapCurrentValueToTarget();
		}
	}
}
