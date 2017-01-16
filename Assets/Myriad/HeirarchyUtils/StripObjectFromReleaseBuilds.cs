using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif // UNITY_EDITOR

public class StripObjectFromReleaseBuilds : MonoBehaviour
{	
#if UNITY_EDITOR
	[PostProcessScene]
	public static void OnPostprocessScene()
	{
		if (Debug.isDebugBuild == false)
		{
			GameObject[] objectsToDestroy = 
				GameObject.FindObjectsOfType<StripObjectFromReleaseBuilds>()
					.Select(elem => elem.gameObject)
					.ToArray();

			foreach (GameObject objectToDestroy in objectsToDestroy)
			{
				if (objectToDestroy != null)
				{
					DestroyImmediate(objectToDestroy);
				}
			}
		}
	}
#endif // UNITY_EDITOR
}

