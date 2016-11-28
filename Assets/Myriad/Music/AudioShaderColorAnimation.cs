using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioShaderColorTargets))]
public class AudioShaderColorAnimation : AudioShaderUniformAnimationBase
{
	public void Awake()
	{
		colorTargets = GetComponent<AudioShaderColorTargets>();
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
		uniformAccessor.SetColor(
			ShaderUniformName,
			GetCurrentUniformValue());
	}

	override protected void SetUniformValueTarget(
		string valueTargetName)
	{
		AudioShaderColorTargets.ColorValueTarget target =
			colorTargets.GetColorValueTarget(valueTargetName);
		
		StartColor = GetCurrentUniformValue();

		EndColor = target.TargetColor;

		BlendTime = target.BlendTime;
		BlendFraction = 0.0f;

		// NOTE: We intentionally maintain our existing BlendVelocity.
	}

	override protected void SnapCurrentValueToTarget()
	{
		BlendFraction = 1.0f;
		BlendVelocity = 0.0f;
	}

	private AudioShaderColorTargets colorTargets = null;
	
	private Color StartColor;
	private Color EndColor;

	private float BlendTime;
	private float BlendFraction;
	private float BlendVelocity;

	private Color GetCurrentUniformValue()
	{
		return LogrithmicColorLerp(
			StartColor, 
			EndColor, 
			BlendFraction);
	}

	private static Color LogrithmicColorLerp(
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
}
