using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioShaderFloatTargets))]
public class AudioShaderFloatAnimation : AudioShaderUniformAnimationBase
{
	public void Awake()
	{
		floatTargets = GetComponent<AudioShaderFloatTargets>();
	}
	
	public void Update()
	{
		BlendFraction = 
			Mathf.SmoothDamp(
				BlendFraction,
				1.0f, // target
				ref BlendVelocity,
				BlendTime);
	}
	
	override protected void CollectShaderUniformValue(
		IAudioShaderUniformAccessor uniformAccessor)
	{
		uniformAccessor.SetFloat(
			ShaderUniformName,
			GetCurrentUniformValue());
	}

	override protected void SetUniformValueTarget(
		string valueTargetName)
	{
		AudioShaderFloatTargets.FloatValueTarget valueTarget =
			floatTargets.GetFloatValueTarget(valueTargetName);
		
		StartValue = GetCurrentUniformValue();

		EndValue = valueTarget.Target;

		BlendTime = valueTarget.BlendTime;
		BlendFraction = 0.0f;

		// NOTE: We intentionally maintain our existing BlendVelocity.
	}

	override protected void SnapCurrentValueToTarget()
	{
		BlendFraction = 1.0f;
		BlendVelocity = 0.0f;
	}

	private AudioShaderFloatTargets floatTargets = null;
	
	private float StartValue;
	private float EndValue;

	private float BlendTime;
	private float BlendFraction;
	private float BlendVelocity;

	private float GetCurrentUniformValue()
	{
		return Mathf.Lerp(StartValue, EndValue, BlendFraction);
	}
}
