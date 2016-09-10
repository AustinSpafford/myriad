using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class SwarmAttractorBase : MonoBehaviour
{
	public struct AttractorState
	{
		public Vector3 Position;
		public Quaternion Rotation;
		public float AttractionScalar;
		public float ThrustScalar; // In the direction of "Rotation * (0, 0, 1)".
	}

	public abstract void AppendActiveAttractors(
		ref List<AttractorState> attractors);
}
