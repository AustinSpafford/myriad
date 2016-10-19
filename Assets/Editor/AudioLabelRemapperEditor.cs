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

	[System.FlagsAttribute]
	private enum LayoutFlags
	{
		None = 0,

		AlignLeft = (1 << 0),
		AlignRight = (1 << 1),
		AlignTop = (1 << 2),
		AlignBottom = (1 << 3),
		
		FillHorizontal = (1 << 4),
		FillVertical = (1 << 5),

		ConsumeHorizontal = (1 << 6),
		ConsumeVertical = (1 << 7),
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

		rect.y += 2; // Nudge the rect downwards to visually center the element.
		
		float rangeFirstWidth = 20.0f;
		float rangeLastWidth = rangeFirstWidth;

		float elasticWidth = Mathf.Max(0.0f, (rect.width - (rangeFirstWidth + rangeLastWidth)));

		float originalLabelNameWidth = (elasticWidth / 2.0f);
		float remappedLabelNameWidth = (elasticWidth / 2.0f);

		// Place the original-label fields.
		{		
			Rect subgroupRect = rect;
			subgroupRect.xMax = rect.center.x;

			// Range, placed right-to-left.
			{
				SerializedProperty originalLabelRangeIsEnabledProperty = 
					rootProperty.FindPropertyRelative("OriginalLabelRangeIsEnabled");

				bool originalLabelRangeIsEnabled = (
					(originalLabelRangeIsEnabledProperty != null) &&
					originalLabelRangeIsEnabledProperty.boolValue);

				if (originalLabelRangeIsEnabled)
				{
					AppendRightAlignedLabelField(
						"]",
						ref subgroupRect);
			
					AppendRightAlignedPropertyField(
						rootProperty,
						"OriginalLabelRangeLast",
						20.0f,
						ref subgroupRect);

					AppendRightAlignedLabelField(
						"..",
						ref subgroupRect);
			
					AppendRightAlignedPropertyField(
						rootProperty,
						"OriginalLabelRangeFirst",
						20.0f,
						ref subgroupRect);

					AppendRightAlignedLabelField(
						"[",
						ref subgroupRect);
				}
			
				AppendRightAlignedPropertyField(
					rootProperty,
					"OriginalLabelRangeIsEnabled",
					14.0f,
					ref subgroupRect);

				AppendRightAlignedLabelField(
					"+",
					ref subgroupRect);
			}

			AppendSpaceFillingPropertyField(
				rootProperty,
				"OriginalLabelPrefix",
				ref subgroupRect);
		}

		// Place the remapped-label fields.
		{
			Rect subgroupRect = rect;
			subgroupRect.xMin = rect.center.x;

			AppendLeftAlignedLabelField(
				" \u2794 ", // Right-arrow glyph.
				ref subgroupRect);
			
			AppendSpaceFillingPropertyField(
				rootProperty,
				"RemappedLabelName",
				ref subgroupRect);
		}
	}

	private static void AppendLeftAlignedPropertyField(
		SerializedProperty rootProperty,
		string propertyName,
		float propertyWidth,
		ref Rect inoutLayoutRect)
	{
		AppendPropertyField(
			rootProperty,
			propertyName,
			new Vector2(propertyWidth, EditorGUIUtility.singleLineHeight),
			(LayoutFlags.AlignLeft | LayoutFlags.AlignTop | LayoutFlags.ConsumeHorizontal),
			ref inoutLayoutRect);
	}

	private static void AppendRightAlignedPropertyField(
		SerializedProperty rootProperty,
		string propertyName,
		float propertyWidth,
		ref Rect inoutLayoutRect)
	{
		AppendPropertyField(
			rootProperty,
			propertyName,
			new Vector2(propertyWidth, EditorGUIUtility.singleLineHeight),
			(LayoutFlags.AlignRight | LayoutFlags.AlignTop | LayoutFlags.ConsumeHorizontal),
			ref inoutLayoutRect);
	}

	private static void AppendSpaceFillingPropertyField(
		SerializedProperty rootProperty,
		string propertyName,
		ref Rect inoutLayoutRect)
	{
		AppendPropertyField(
			rootProperty,
			propertyName,
			new Vector2(0.0f, EditorGUIUtility.singleLineHeight),
			(LayoutFlags.AlignTop | LayoutFlags.FillHorizontal),
			ref inoutLayoutRect);
	}

	private static void AppendPropertyField(
		SerializedProperty rootProperty,
		string propertyName,
		Vector2 baseElementSize,
		LayoutFlags layoutFlags,
		ref Rect inoutLayoutRect)
	{
		SerializedProperty childProperty = rootProperty.FindPropertyRelative(propertyName);

		if (childProperty == null)
		{
			Debug.LogErrorFormat("Unable to find a property named [{0}].", propertyName);
		}
		else
		{
			EditorGUI.PropertyField(
				BuildLayoutRectAndConsume(baseElementSize, layoutFlags, ref inoutLayoutRect),
				childProperty,
				GUIContent.none);
		}
	}

	private static void AppendLeftAlignedLabelField(
		string labelText,
		ref Rect inoutLayoutRect)
	{
		AppendLabelField(
			new GUIContent(labelText),
			(LayoutFlags.AlignLeft | LayoutFlags.AlignTop | LayoutFlags.ConsumeHorizontal),
			ref inoutLayoutRect);
	}
	

	private static void AppendRightAlignedLabelField(
		string labelText,
		ref Rect inoutLayoutRect)
	{
		AppendLabelField(
			new GUIContent(labelText),
			(LayoutFlags.AlignRight | LayoutFlags.AlignTop | LayoutFlags.ConsumeHorizontal),
			ref inoutLayoutRect);
	}

	private static void AppendLabelField(
		GUIContent labelContent,
		LayoutFlags layoutFlags,
		ref Rect inoutLayoutRect)
	{
		Vector2 labelSize = GUI.skin.label.CalcSize(labelContent);
		
		EditorGUI.LabelField(
			BuildLayoutRectAndConsume(labelSize, layoutFlags, ref inoutLayoutRect),
			labelContent);
	}

	private static Rect BuildLayoutRectAndConsume(
		Vector2 baseElementSize,
		LayoutFlags layoutFlags,
		ref Rect inoutLayoutRect)
	{
		Vector2 finalElementSize = baseElementSize;

		finalElementSize.x = Mathf.Min(finalElementSize.x, inoutLayoutRect.width);
		finalElementSize.y = Mathf.Min(finalElementSize.y, inoutLayoutRect.height);

		if ((layoutFlags & LayoutFlags.FillHorizontal) != 0)
		{
			finalElementSize.x = inoutLayoutRect.width;
		}

		if ((layoutFlags & LayoutFlags.FillVertical) != 0)
		{
			finalElementSize.y = inoutLayoutRect.height;
		}

		Rect elementRect = new Rect();
		
		// Process the horizontal-layout.
		if ((layoutFlags & LayoutFlags.AlignLeft) != 0)
		{
			elementRect.xMin = inoutLayoutRect.xMin;
			elementRect.xMax = (inoutLayoutRect.xMin + finalElementSize.x);
			
			if ((layoutFlags & LayoutFlags.ConsumeHorizontal) != 0)
			{
				inoutLayoutRect.xMin += finalElementSize.x;
			}
		}
		else if ((layoutFlags & LayoutFlags.AlignRight) != 0)
		{
			elementRect.xMin = (inoutLayoutRect.xMax - finalElementSize.x);
			elementRect.xMax = inoutLayoutRect.xMax;
			
			if ((layoutFlags & LayoutFlags.ConsumeHorizontal) != 0)
			{
				inoutLayoutRect.xMax -= finalElementSize.x;
			}
		}
		else
		{
			elementRect.xMin = (inoutLayoutRect.center.x - (finalElementSize.x / 2.0f));
			elementRect.xMax = (inoutLayoutRect.center.x + (finalElementSize.x / 2.0f));
			
			if ((layoutFlags & LayoutFlags.ConsumeHorizontal) != 0)
			{
				Debug.LogError("Unable to consume horizontal space while horizontally-centered.");
			}
		}
		
		// Process the vertical-layout.
		if ((layoutFlags & LayoutFlags.AlignTop) != 0)
		{
			elementRect.yMin = inoutLayoutRect.yMin;
			elementRect.yMax = (inoutLayoutRect.yMin + finalElementSize.y);
			
			if ((layoutFlags & LayoutFlags.ConsumeVertical) != 0)
			{
				inoutLayoutRect.yMin += finalElementSize.y;
			}
		}
		else if ((layoutFlags & LayoutFlags.AlignBottom) != 0)
		{
			elementRect.yMin = (inoutLayoutRect.yMax - finalElementSize.y);
			elementRect.yMax = inoutLayoutRect.yMax;
			
			if ((layoutFlags & LayoutFlags.ConsumeVertical) != 0)
			{
				inoutLayoutRect.yMax += finalElementSize.y;
			}
		}
		else
		{
			elementRect.yMin = (inoutLayoutRect.center.y - (finalElementSize.y / 2.0f));
			elementRect.yMax = (inoutLayoutRect.center.y + (finalElementSize.y / 2.0f));
			
			if ((layoutFlags & LayoutFlags.ConsumeVertical) != 0)
			{
				Debug.LogError("Unable to consume vertical space while vertically-centered.");
			}
		}

		return elementRect;
	}
}
