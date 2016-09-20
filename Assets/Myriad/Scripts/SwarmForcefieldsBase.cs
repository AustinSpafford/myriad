using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class SwarmForcefieldsBase : MonoBehaviour
{
	public abstract void AppendActiveForcefields(
		ref List<SwarmShaderForcefieldState> forcefields);
}
