using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class SwarmAttractorBase : MonoBehaviour
{
	public struct AttractorState
	{
		public Vector3 Position;
		public Quaternion Rotation;
		public float UnitizedGravity;
		public float ThrustScalar; // Pointing in the direction of "rotation".
	}

	public abstract void AppendActiveAttractors(
		ref List<AttractorState> attractors);
}
