using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioShaderColorTargets))]
public class AudioShaderColorAnimation : AudioShaderColorAnimationBase
{
	public void Awake()
	{
		audioShaderColorTargets = GetComponent<AudioShaderColorTargets>();
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
			GetCurrentAudioColor());
	}

	override protected void SetUniformValueTarget(
		string valueTargetName)
	{
		AudioShaderColorTargets.ColorValueTarget valueTarget =
			audioShaderColorTargets.GetColorValueTarget(valueTargetName);
		
		StartColor = GetCurrentAudioColor();

		EndColor = valueTarget.TargetColor;

		BlendTime = valueTarget.BlendTime;
		BlendFraction = 0.0f;

		// NOTE: We intentionally maintain our existing BlendVelocity.
	}

	override protected void SnapCurrentValueToTarget()
	{
		BlendFraction = 1.0f;
		BlendVelocity = 0.0f;
	}

	private AudioShaderColorTargets audioShaderColorTargets = null;
	
	private Color StartColor;
	private Color EndColor;

	private float BlendTime;
	private float BlendFraction;
	private float BlendVelocity;

	private Color GetCurrentAudioColor()
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
