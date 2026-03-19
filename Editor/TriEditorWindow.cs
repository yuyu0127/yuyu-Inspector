#region カスタマイズ: TriEditorWindow対応

using TriInspector.Utilities;
using UnityEditor;
using UnityEngine;

namespace TriInspector
{
    [HideMonoScript]
    public abstract class TriEditorWindow : EditorWindow
    {
        [SerializeField, HideInInspector,]
        private Vector2 _scrollPosition;

        [SerializeField, HideInInspector,]
        private Editor _editor;

        private void OnGUI()
        {
            Editor.CreateCachedEditor(this, null, ref _editor);
            using (var scrollViewScope =
                   new EditorGUILayout.ScrollViewScope(_scrollPosition, EditorStyles.inspectorFullWidthMargins))
            {
                _scrollPosition = scrollViewScope.scrollPosition;
                var labelWidth = TriSessionState.LabelWidth;
                var clamped = Mathf.Clamp(labelWidth, 1, position.width - 20);
                EditorGUIUtility.labelWidth = clamped;

                _editor.OnInspectorGUI();
            }
        }
    }
}

#endregion