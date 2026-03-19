using System;
using TriInspector.Utilities;
using UnityEditor;
using UnityEngine;

namespace TriInspector.Elements
{
    public class TriPropertyElement : TriElement
    {
        private readonly TriProperty _property;

        [Serializable]
        public struct Props
        {
            public bool forceInline;
        }

        public TriPropertyElement(TriProperty property, Props props = default)
        {
            _property = property;

            foreach (var error in _property.ExtensionErrors)
            {
                AddChild(new TriInfoBoxElement(error, TriMessageType.Error));
            }

            var element = CreateElement(property, props);

            var drawers = property.AllDrawers;
            for (var index = drawers.Count - 1; index >= 0; index--)
            {
                element = drawers[index].CreateElementInternal(property, element);
            }

            AddChild(element);
        }

        public override float GetHeight(float width)
        {
            #region カスタマイズ: TriEditorWindow対応

            if (_property.RawName == "m_SerializedDataModeController")
            {
                return 0;
            }

            #endregion

            if (!_property.IsVisible)
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }

            return base.GetHeight(width);
        }

        public override void OnGUI(Rect position)
        {
            #region カスタマイズ: TriEditorWindow対応

            if (_property.RawName == "m_SerializedDataModeController")
            {
                return;
            }

            #endregion

            if (!_property.IsVisible)
            {
                return;
            }

            var oldShowMixedValue = EditorGUI.showMixedValue;
            var oldEnabled = GUI.enabled;

            GUI.enabled &= _property.IsEnabled;
            EditorGUI.showMixedValue = _property.IsValueMixed;
            var overrideCtx = TriPropertyOverrideContext.BeginProperty();

            #region カスタマイズ: ラベル幅を調整可能にする

            if (_property.DisplayNameContent != GUIContent.none)
            {
                var delta = DrawDragLine(position.x + EditorGUIUtility.labelWidth, position.y,
                    EditorGUIUtility.singleLineHeight, new Color(1, 1, 1, 0.5f));
                if (delta != 0)
                {
                    var newValue = TriSessionState.LabelWidth + delta;
                    newValue = Mathf.Max(1, newValue);
                    TriSessionState.LabelWidth = newValue;
                }
            }

            #endregion

            if (_property.TryGetSerializedProperty(out var serializedProperty))
            {
                EditorGUI.BeginProperty(position, null, serializedProperty);
            }

            base.OnGUI(position);

            if (_property.TryGetSerializedProperty(out _))
            {
                EditorGUI.EndProperty();
            }

            overrideCtx.EndProperty();
            EditorGUI.showMixedValue = oldShowMixedValue;
            GUI.enabled = oldEnabled;
        }

        #region カスタマイズ: ラベル幅を調整可能にする

        private static float DrawDragLine(float x, float y, float height, Color color)
        {
            var lineRect = new Rect(x - 1, y, 1, height);
            var id = GUIUtility.GetControlID(FocusType.Passive);

            var draggableRect = new Rect(lineRect)
            {
                xMin = lineRect.xMin - 5,
                xMax = lineRect.xMax + 5,
            };
            EditorGUIUtility.AddCursorRect(draggableRect, MouseCursor.ResizeHorizontal);

            var isHover = draggableRect.Contains(Event.current.mousePosition);
            if (isHover || GUIUtility.hotControl == id)
            {
                var c = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture);
                GUI.color = c;
            }

            // ドラッグ検出して移動量を返す
            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    if (draggableRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        var delta = e.delta.x;
                        e.Use();
                        return delta;
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }

                    break;
                }
            }

            return 0f;
        }

        #endregion

        private static TriElement CreateElement(TriProperty property, Props props)
        {
            switch (property.PropertyType)
            {
                case TriPropertyType.Array:
                {
                    return CreateArrayElement(property);
                }

                case TriPropertyType.Reference:
                {
                    return CreateReferenceElement(property, props);
                }

                case TriPropertyType.Generic:
                {
                    return CreateGenericElement(property, props);
                }

                default:
                {
                    return new TriNoDrawerElement(property);
                }
            }
        }

        private static TriElement CreateArrayElement(TriProperty property)
        {
            return new TriListElement(property);
        }

        private static TriElement CreateReferenceElement(TriProperty property, Props props)
        {
            if (property.TryGetAttribute(out InlinePropertyAttribute inlineAttribute))
            {
                return new TriReferenceElement(property, new TriReferenceElement.Props
                {
                    inline = true,
                    drawPrefixLabel = !props.forceInline,
                    labelWidth = inlineAttribute.LabelWidth,
                });
            }

            if (props.forceInline)
            {
                return new TriReferenceElement(property, new TriReferenceElement.Props
                {
                    inline = true,
                    drawPrefixLabel = false,
                });
            }

            return new TriReferenceElement(property, new TriReferenceElement.Props
            {
                inline = false,
                drawPrefixLabel = false,
            });
        }

        private static TriElement CreateGenericElement(TriProperty property, Props props)
        {
            if (property.TryGetAttribute(out InlinePropertyAttribute inlineAttribute))
            {
                return new TriInlineGenericElement(property, new TriInlineGenericElement.Props
                {
                    drawPrefixLabel = !props.forceInline,
                    labelWidth = inlineAttribute.LabelWidth,
                });
            }

            if (props.forceInline)
            {
                return new TriInlineGenericElement(property, new TriInlineGenericElement.Props
                {
                    drawPrefixLabel = false,
                });
            }

            return new TriFoldoutElement(property);
        }
    }
}