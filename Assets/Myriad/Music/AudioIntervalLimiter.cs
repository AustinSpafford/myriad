using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AudioIntervalLimiter : MonoBehaviour
{
	public float IntervalStartTime = 0.0f;
	public float IntervalDuration = 999.0f;

	public float FadeInDuration = 1.0f;
	public float FadeOutDuration = 1.0f;
	
	public float IntervalEndTime { get { return (IntervalStartTime + IntervalDuration); } }
	
	public void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void OnEnable()
	{
		originalVolume = audioSource.volume;
	}

	public void OnDisable()
	{
		audioSource.volume = originalVolume;
	}

	public void Update()
	{
		// Move the audio's playhead.
		if (audioSource.isPlaying)
		{
			if (audioSource.time < IntervalStartTime)
			{
				if (audioSource.pitch > 0.0f)
				{
					audioSource.time = IntervalStartTime;
				}
				else
				{
					if (audioSource.loop)
					{
						audioSource.time = (
							IntervalStartTime +
							Mathf.Repeat((audioSource.time - IntervalStartTime), IntervalDuration));
					}
					else
					{
						audioSource.Stop();
					}
				}
			}
			else if (audioSource.time > IntervalEndTime)
			{
				if (audioSource.pitch > 0.0f)
				{
					if (audioSource.loop)
					{
						audioSource.time = (
							IntervalStartTime +
							Mathf.Repeat((audioSource.time - IntervalStartTime), IntervalDuration));
					}
					else
					{
						audioSource.Stop();
					}
				}
				else
				{
					audioSource.time = IntervalEndTime;
				}
			}
		}

		// Adjust the audio's volume.
		if (audioSource.isPlaying)
		{
			float fadeInFraction = 
				(FadeInDuration > 0.0f) ?
					Mathf.Clamp01(Mathf.InverseLerp(
						IntervalStartTime, 
						(IntervalStartTime + FadeInDuration),
						audioSource.time)) :
					1.0f;
			
			float fadeOutFraction = 
				(FadeOutDuration > 0.0f) ?
					Mathf.Clamp01(Mathf.InverseLerp(
						IntervalEndTime, 
						(IntervalEndTime - FadeOutDuration),
						audioSource.time)) :
					1.0f;

			audioSource.volume = (originalVolume * fadeInFraction * fadeOutFraction);
		}
	}
	
	private AudioSource audioSource = null;

	private float originalVolume = 1.0f;
}
