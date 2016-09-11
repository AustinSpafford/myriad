using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class SwarmAttractorBase : MonoBehaviour
{
	public abstract void AppendActiveAttractors(
		ref List<SwarmShaderAttractorState> attractors);
}
