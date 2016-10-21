using UnityEngine;
using UnityEditor;
using UnityEditorInternal;  
using System.Collections.Generic;

// Many thanks to the guide on ReorderableList at: http://va.lent.in/unity-make-your-lists-functional-with-reorderablelist/

[CustomEditor(typeof(AudioShaderColorAnimation))]
public class AudioShaderColorAnimationEditor : Editor
{
	public void OnEnable()
	{
		colorEventsList = 
			new ReorderableList(
				serializedObject,
				serializedObject.FindProperty("audioColorEvents"),
				draggable: true,
				displayHeader: true,
				displayAddButton: true,
				displayRemoveButton: true);

		colorEventsList.drawHeaderCallback = 
			(Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Audio Color Events");
			};

		colorEventsList.onAddCallback =
			(ReorderableList list) =>
			{
				int newElementIndex = list.serializedProperty.arraySize++;
				SerializedProperty property = list.serializedProperty.GetArrayElementAtIndex(newElementIndex);

				property.FindPropertyRelative("ShaderUniformName").stringValue = "";
				property.FindPropertyRelative("AudioLabelName").stringValue = "";
				property.FindPropertyRelative("LabelStartColor").colorValue = Color.white;
				property.FindPropertyRelative("LabelStartBlendTime").floatValue = 0.1f;
				property.FindPropertyRelative("LabelEndColor").colorValue = Color.black;
				property.FindPropertyRelative("LabelEndBlendTime").floatValue = 0.1f;
			};

		colorEventsList.drawElementCallback =
			(Rect rect, int index, bool isActive, bool isFocused) =>
			{
				SerializedProperty property = colorEventsList.serializedProperty.GetArrayElementAtIndex(index);

				property.isExpanded = true;

				rect.xMin += 10.0f;

				EditorGUI.PropertyField(
					rect, 
					property,
					GUIContent.none,
					includeChildren: true);
			};

		colorEventsList.elementHeightCallback =
			(int index) =>
			{
				SerializedProperty property = colorEventsList.serializedProperty.GetArrayElementAtIndex(index);
				
				property.isExpanded = true;

				return EditorGUI.GetPropertyHeight(property);
			};
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		colorEventsList.DoLayoutList();

		serializedObject.ApplyModifiedProperties();
	}

	private ReorderableList colorEventsList = null;
}
