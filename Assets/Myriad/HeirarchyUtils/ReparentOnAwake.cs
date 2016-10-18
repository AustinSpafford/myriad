using UnityEngine;
using System.Collections;

public class ReparentOnAwake : MonoBehaviour
{
	public Transform NewParent = null;

	public void Awake()
	{
		if (NewParent != null)
		{
			transform.SetParent(NewParent);

			// Signify that we've completed our task and are now dormant.
			NewParent = null;
			enabled = false;
		}
	}
}
