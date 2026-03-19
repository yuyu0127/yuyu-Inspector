using UnityEditor;
using UnityEngine;

namespace TriInspector.Utilities
{
    public static partial class TriEditorGUI
    {
        public static void Foldout(Rect rect, TriProperty property)
        {
            var content = property.DisplayNameContent;
            if (property.TryGetSerializedProperty(out var serializedProperty))
            {
                EditorGUI.BeginProperty(rect, content, serializedProperty);

                #region カスタマイズ: Altキー押下時に子要素も展開する

                // property.IsExpanded = EditorGUI.Foldout(rect, property.IsExpanded, content, true);
                EditorGUI.BeginChangeCheck();
                var style = new GUIStyle(EditorStyles.foldout) {richText = true};
                var expanded = EditorGUI.Foldout(rect, property.IsExpanded, content, true, style);
                if (EditorGUI.EndChangeCheck())
                {
                    property.SetExpanded(expanded, Event.current.alt);
                }

                #endregion

                EditorGUI.EndProperty();
            }
            else
            {
                #region カスタマイズ: Altキー押下時に子要素も展開する

                // property.IsExpanded = EditorGUI.Foldout(rect, property.IsExpanded, content, true);
                EditorGUI.BeginChangeCheck();
                var expanded = EditorGUI.Foldout(rect, property.IsExpanded, content, true);
                if (EditorGUI.EndChangeCheck())
                {
                    property.SetExpanded(expanded, Event.current.alt);
                }

                #endregion
            }
        }

        public static void DrawBox(Rect position, GUIStyle style,
            bool isHover = false, bool isActive = false, bool on = false, bool hasKeyboardFocus = false)
        {
            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(position, GUIContent.none, isHover, isActive, on, hasKeyboardFocus);
            }
        }
    }
}