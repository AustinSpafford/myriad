using UnityEngine;
using System;
using System.Collections;

public class AudioIntervalLimiter : MonoBehaviour
{
	public float StartTime = 0.0f;
	public float EndTime = 999.0f;
	
	public void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void Update()
	{
		if (audioSource.isPlaying)
		{
			if (audioSource.time < StartTime)
			{
				audioSource.time = StartTime;
			}
			else if (audioSource.time > EndTime)
			{
				if (audioSource.loop)
				{
					audioSource.time = StartTime;
				}
				else
				{
					audioSource.Stop();
				}
			}			
		}
	}
	
	private AudioSource audioSource = null;  
}
