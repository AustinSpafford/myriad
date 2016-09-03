using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

[ExecuteInEditMode]
public class SelectionGizmo : MonoBehaviour
{
	public enum GizmoShape
	{
		WireCube,
		WireSphere,
	};

	public GizmoShape Shape = GizmoShape.WireCube;
	public Color GizmoColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

	public Vector3 LocalPosition = Vector3.zero;
	public Vector3 LocalRotation = Vector3.zero;
	public Vector3 LocalScale = Vector3.one;

#if UNITY_EDITOR
	public void OnDrawGizmosSelected()
	{
		if (EditorApplication.isPlaying == false)
		{
			Gizmos.color = GizmoColor;
			
			Gizmos.matrix = (
				transform.localToWorldMatrix * 
				Matrix4x4.TRS(
					LocalPosition, 
					Quaternion.Euler(LocalRotation), 
					LocalScale));

			switch (Shape)
			{
				case GizmoShape.WireCube:
					Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
					break;
					
				case GizmoShape.WireSphere:
					Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
					break;

				default:
					throw new System.ComponentModel.InvalidEnumArgumentException();
			}
		}
	}
#endif // UNITY_EDITOR
}
