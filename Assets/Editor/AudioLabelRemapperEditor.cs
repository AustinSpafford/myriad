using UnityEngine;
using UnityEditor;
using UnityEditorInternal;  
using System.Collections.Generic;

// Many thanks to the guide on ReorderableList at: http://va.lent.in/unity-make-your-lists-functional-with-reorderablelist/

[CustomEditor(typeof(AudioLabelRemapper))]
public class AudioLabelRemapperEditor : Editor
{
	public void OnEnable()
	{
		labelRemappingsReorderableList = 
			new ReorderableList(
				serializedObject,
				serializedObject.FindProperty("LabelRemappings"),
				draggable: true,
				displayHeader: true,
				displayAddButton: true,
				displayRemoveButton: true);

		labelRemappingsReorderableList.drawHeaderCallback = 
			(Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Label Remappings");
			};

		labelRemappingsReorderableList.drawElementCallback = DrawLabelRemappingElement;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		labelRemappingsReorderableList.DoLayoutList();

		serializedObject.ApplyModifiedProperties();
	}

	private ReorderableList labelRemappingsReorderableList = null;

	private void DrawLabelRemappingElement(
		Rect rect, 
		int index, 
		bool isActive, 
		bool isFocused)
	{
		SerializedProperty rootProperty = 
			labelRemappingsReorderableList.serializedProperty.GetArrayElementAtIndex(index);
		
		// Place the original-label fields.
		{		
			Rect subgroupRect = rect;
			subgroupRect.xMax = rect.center.x;

			// Numeric-range, placed right-to-left.
			{
				SerializedProperty originalLabelRangeIsEnabledProperty = 
					rootProperty.FindPropertyRelative("OriginalLabelRangeIsEnabled");

				bool originalLabelRangeIsEnabled = (
					(originalLabelRangeIsEnabledProperty != null) &&
					originalLabelRangeIsEnabledProperty.boolValue);

				if (originalLabelRangeIsEnabled)
				{
					AppendRightAlignedLabelField(
						new GUIContent("]"),
						ref subgroupRect);
			
					AppendRightAlignedPropertyField(
						rootProperty.FindPropertyRelative("OriginalLabelRangeLast"),
						20.0f,
						ref subgroupRect);

					AppendRightAlignedLabelField(
						new GUIContent(".."),
						ref subgroupRect);
			
					AppendRightAlignedPropertyField(
						rootProperty.FindPropertyRelative("OriginalLabelRangeFirst"),
						20.0f,
						ref subgroupRect);

					AppendRightAlignedLabelField(
						new GUIContent("["),
						ref subgroupRect);
				}
			
				AppendRightAlignedPropertyField(
					rootProperty.FindPropertyRelative("OriginalLabelRangeIsEnabled"),
					14.0f,
					ref subgroupRect);

				AppendRightAlignedLabelField(
					new GUIContent("+"),
					ref subgroupRect);
			}
			
			AppendSpaceFillingPropertyField(
				rootProperty.FindPropertyRelative("OriginalLabelPrefix"),
				ref subgroupRect);
		}

		// Place the remapped-label fields.
		{
			Rect subgroupRect = rect;
			subgroupRect.xMin = rect.center.x;

			AppendLeftAlignedLabelField(
				new GUIContent(" \u2794 "), // Right-arrow glyph.
				ref subgroupRect);
			
			AppendSpaceFillingPropertyField(
				rootProperty.FindPropertyRelative("RemappedLabelName"),
				ref subgroupRect);
		}
	}

	private static void AppendLeftAlignedPropertyField(
		SerializedProperty property,
		float propertyWidth,
		ref Rect inoutLayoutRect)
	{
		Rect fieldRect = new Rect(inoutLayoutRect);
		fieldRect.xMax = (fieldRect.xMin + propertyWidth);
		VerticallyCenterFieldRect(EditorGUI.GetPropertyHeight(property, GUIContent.none), ref fieldRect);

		inoutLayoutRect.xMin += propertyWidth;

		EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
	}

	private static void AppendRightAlignedPropertyField(
		SerializedProperty property,
		float propertyWidth,
		ref Rect inoutLayoutRect)
	{
		Rect fieldRect = new Rect(inoutLayoutRect);
		fieldRect.xMin = (fieldRect.xMax - propertyWidth);
		VerticallyCenterFieldRect(EditorGUI.GetPropertyHeight(property, GUIContent.none), ref fieldRect);
		
		inoutLayoutRect.xMax -= propertyWidth;

		EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
	}

	private static void AppendSpaceFillingPropertyField(
		SerializedProperty property,
		ref Rect inoutLayoutRect)
	{
		Rect fieldRect = new Rect(inoutLayoutRect);
		VerticallyCenterFieldRect(EditorGUI.GetPropertyHeight(property, GUIContent.none), ref fieldRect);

		inoutLayoutRect.xMin = inoutLayoutRect.xMax;
		
		EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
	}

	private static void AppendLeftAlignedLabelField(
		GUIContent labelContent,
		ref Rect inoutLayoutRect)
	{
		Vector2 labelSize = GUI.skin.label.CalcSize(labelContent);
		
		Rect fieldRect = new Rect(inoutLayoutRect);
		fieldRect.xMax = (fieldRect.xMin + labelSize.x);
		VerticallyCenterFieldRect(labelSize.y, ref fieldRect);

		inoutLayoutRect.xMin += labelSize.x;

		EditorGUI.LabelField(fieldRect, labelContent);
	}

	private static void AppendRightAlignedLabelField(
		GUIContent labelContent,
		ref Rect inoutLayoutRect)
	{
		Vector2 labelSize = GUI.skin.label.CalcSize(labelContent);
		
		Rect fieldRect = new Rect(inoutLayoutRect);
		fieldRect.xMin = (fieldRect.xMax - labelSize.x);
		VerticallyCenterFieldRect(labelSize.y, ref fieldRect);

		inoutLayoutRect.xMax -= labelSize.x;

		EditorGUI.LabelField(fieldRect, labelContent);
	}
	
	private static void VerticallyCenterFieldRect(
		float fieldHeight,
		ref Rect inoutFieldRect)
	{
		inoutFieldRect.yMin = (inoutFieldRect.center.y - (fieldHeight / 2.0f));
		inoutFieldRect.yMax = (inoutFieldRect.yMin + fieldHeight);
	}
}
