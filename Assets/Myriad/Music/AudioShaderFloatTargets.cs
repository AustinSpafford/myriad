using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AudioShaderFloatTargets : MonoBehaviour
{
	[System.Serializable]
	public class FloatValueTarget
	{
		public string ValueTargetName;

		public float Target;
		public float BlendTime;
	}

	public bool DebugEnabled = false;
	
	public FloatValueTarget GetFloatValueTarget(
		string valueTargetName)
	{
		FloatValueTarget result =
			floatTargets
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
	private List<FloatValueTarget> floatTargets = new List<FloatValueTarget>();
}
