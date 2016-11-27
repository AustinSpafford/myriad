using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AudioShaderColorTargets : MonoBehaviour
{
	[System.Serializable]
	public class ColorValueTarget
	{
		public string ValueTargetName;

		public Color TargetColor = Color.black;
		public float BlendTime;
	}

	public bool DebugEnabled = false;
	
	public ColorValueTarget GetColorValueTarget(
		string valueTargetName)
	{
		ColorValueTarget result =
			colorTargets
				.Where(elem => (elem.ValueTargetName == valueTargetName))
				.SingleOrDefault();

		if (result == null)
		{
			throw new System.InvalidOperationException(
				string.Format("There's no value-target by the name of [{0}].", valueTargetName));
		}

		return result;
	}

	[SerializeField]
	private List<ColorValueTarget> colorTargets = new List<ColorValueTarget>();
}
