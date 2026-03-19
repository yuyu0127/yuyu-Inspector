using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriInspector.Resolvers;
using TriInspectorUnityInternalBridge;
using TriInspector.Utilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TriInspector.Elements
{
    public class TriListElement : TriElement
    {
        private const int MinElementsForVirtualization = 25;

        private const float ListExtraWidth = 7f;
        private const float DraggableAreaExtraWidth = 14f;

        private readonly TriProperty _property;
        private readonly ReorderableList _reorderableListGui;
        private readonly bool _alwaysExpanded;
        private readonly bool _showElementLabels;
        private readonly bool _showAlternatingBackground;

        #region カスタマイズ: テーブル対応

        private readonly bool _table;
        private readonly TriPropertyOverrideContext _tableListPropertyOverrideContext;
        private float[] _columnWidths;

        #endregion

        #region カスタマイズ: 交互の背景色

        private readonly bool _alternatingRowBackgrounds;

        #endregion

        private float _lastContentWidth;
        private int? _lastInvisibleElement;
        private int? _lastVisibleElement;

        protected ReorderableList ListGui => _reorderableListGui;

        public TriListElement(TriProperty property)
        {
            property.TryGetAttribute(out ListDrawerSettingsAttribute settings);

            _property = property;
            _alwaysExpanded = settings?.AlwaysExpanded ?? false;

            #region カスタマイズ: ToStringメソッドがoverrideされている場合、リスト要素のラベルとして利用する

            // _showElementLabels = settings?.ShowElementLabels ?? false;
            _showElementLabels = !IsToStringMethodOverridden(property);

            #endregion

            _showAlternatingBackground = settings?.ShowAlternatingBackground ?? true;

            #region カスタマイズ: テーブル対応

            _table = settings != null && settings.Table;
            if (_table)
            {
                _tableListPropertyOverrideContext = new TableListPropertyOverrideContext(_property);
            }

            #endregion

            #region カスタマイズ: 交互の背景色

            _alternatingRowBackgrounds = settings?.AlternatingRowBackgrounds ?? false;

            #endregion

            _reorderableListGui = new ReorderableList(null, _property.ArrayElementType)
            {
                showDefaultBackground = settings?.ShowDefaultBackground ?? true,
                draggable = settings?.Draggable ?? true,
                displayAdd = settings == null || !settings.HideAddButton,
                displayRemove = settings == null || !settings.HideRemoveButton,
                drawHeaderCallback = DrawHeaderCallback,
                elementHeightCallback = ElementHeightCallback,
                drawElementBackgroundCallback = DrawElementBackgroundCallback,
                drawElementCallback = DrawElementCallback,
                onAddCallback = AddElementCallback,
                onRemoveCallback = RemoveElementCallback,
                onReorderCallbackWithDetails = ReorderCallback,
            };

            if (!_reorderableListGui.displayAdd && !_reorderableListGui.displayRemove)
            {
                _reorderableListGui.footerHeight = 0f;
            }
        }

        public override bool Update()
        {
            var dirty = false;

            if (_property.TryGetSerializedProperty(out var serializedProperty) && serializedProperty.isArray)
            {
                _reorderableListGui.serializedProperty = serializedProperty;
            }
            else if (_property.Value != null)
            {
                _reorderableListGui.list = (IList) _property.Value;
            }
            else if (_reorderableListGui.list == null)
            {
                _reorderableListGui.list = (IList) (_property.FieldType.IsArray
                    ? Array.CreateInstance(_property.ArrayElementType, 0)
                    : Activator.CreateInstance(_property.FieldType));
            }

            if (_alwaysExpanded && !_property.IsExpanded)
            {
                _property.IsExpanded = true;
            }

            #region カスタマイズ: テーブル対応

            _reorderableListGui.headerHeight = _table
                ? _property.IsExpanded
                    ? _reorderableListGui.count == 0
                        ? 20
                        : 32
                    : 18
                : 20;

            #endregion

            if (_property.IsExpanded)
            {
                dirty |= GenerateChildren();
            }
            else
            {
                dirty |= ClearChildren();
            }

            dirty |= base.Update();

            if (dirty)
            {
                ReorderableListProxy.ClearCacheRecursive(_reorderableListGui);
            }

            return dirty;
        }

        public override float GetHeight(float width)
        {
            if (!_property.IsExpanded)
            {
                return _reorderableListGui.headerHeight + 4f;
            }

            _lastContentWidth = width;

            return _reorderableListGui.GetHeight();
        }

        public override void OnGUI(Rect position)
        {
            if (!_property.IsExpanded)
            {
                _lastInvisibleElement = null;
                _lastVisibleElement = null;

                ReorderableListProxy.DoListHeader(_reorderableListGui, new Rect(position)
                {
                    yMax = position.yMax - 4,
                });
                return;
            }

            if (_reorderableListGui.count < MinElementsForVirtualization)
            {
                _lastInvisibleElement = null;
                _lastVisibleElement = null;
            }

            var labelWidthExtra = ListExtraWidth + DraggableAreaExtraWidth;

            using (TriGuiHelper.PushLabelWidth(EditorGUIUtility.labelWidth - labelWidthExtra))
            {
                _reorderableListGui.DoList(position);
            }
        }

        private void AddElementCallback(ReorderableList reorderableList)
        {
            AddElementCallback(reorderableList, null);
        }

        private void AddElementCallback(ReorderableList reorderableList, Object addedReferenceValue)
        {
            if (_property.TryGetSerializedProperty(out _))
            {
                ReorderableListProxy.DoAddButton(reorderableList, addedReferenceValue);
                _property.NotifyValueChanged();
                return;
            }

            var template = CloneValue(_property);

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (_property.FieldType.IsArray)
                {
                    var array = Array.CreateInstance(_property.ArrayElementType, template.Length + 1);
                    Array.Copy(template, array, template.Length);

                    if (addedReferenceValue != null)
                    {
                        array.SetValue(addedReferenceValue, array.Length - 1);
                    }

                    value = array;
                }
                else
                {
                    if (value == null)
                    {
                        value = (IList) Activator.CreateInstance(_property.FieldType);
                    }

                    var newElement = addedReferenceValue != null
                        ? addedReferenceValue
                        : CreateDefaultElementValue(_property);

                    value.Add(newElement);
                }

                return value;
            });
        }

        private void RemoveElementCallback(ReorderableList reorderableList)
        {
            if (_property.TryGetSerializedProperty(out _))
            {
                ReorderableListProxy.defaultBehaviours.DoRemoveButton(reorderableList);
                _property.NotifyValueChanged();
                return;
            }

            var template = CloneValue(_property);
            var ind = reorderableList.index;

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (_property.FieldType.IsArray)
                {
                    var array = Array.CreateInstance(_property.ArrayElementType, template.Length - 1);
                    Array.Copy(template, 0, array, 0, ind);
                    Array.Copy(template, ind + 1, array, ind, array.Length - ind);
                    value = array;
                }
                else
                {
                    value?.RemoveAt(ind);
                }

                return value;
            });
        }

        private void ReorderCallback(ReorderableList list, int oldIndex, int newIndex)
        {
            if (_property.TryGetSerializedProperty(out _))
            {
                _property.NotifyValueChanged();
                return;
            }

            var mainValue = _property.Value;

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (value == mainValue)
                {
                    return value;
                }

                var element = value[oldIndex];
                for (var index = 0; index < value.Count - 1; ++index)
                {
                    if (index >= oldIndex)
                    {
                        value[index] = value[index + 1];
                    }
                }

                for (var index = value.Count - 1; index > 0; --index)
                {
                    if (index > newIndex)
                    {
                        value[index] = value[index - 1];
                    }
                }

                value[newIndex] = element;

                return value;
            });
        }

        private void SetArraySizeCallback(int arraySize)
        {
            if (arraySize < 0)
            {
                return;
            }

            if (_property.TryGetSerializedProperty(out var serializedProperty))
            {
                serializedProperty.arraySize = arraySize;
                _property.NotifyValueChanged();
                return;
            }

            var template = CloneValue(_property);

            _property.SetValues(targetIndex =>
            {
                var value = (IList) _property.GetValue(targetIndex);

                if (_property.FieldType.IsArray)
                {
                    var array = Array.CreateInstance(_property.ArrayElementType, arraySize);
                    Array.Copy(template, array, Math.Min(arraySize, template.Length));

                    value = array;
                }
                else
                {
                    if (value == null)
                    {
                        value = (IList) Activator.CreateInstance(_property.FieldType);
                    }

                    while (value.Count > arraySize)
                    {
                        value.RemoveAt(value.Count - 1);
                    }

                    while (value.Count < arraySize)
                    {
                        var newElement = CreateDefaultElementValue(_property);
                        value.Add(newElement);
                    }
                }

                return value;
            });
        }

        private bool GenerateChildren()
        {
            var count = _reorderableListGui.count;

            if (ChildrenCount == count)
            {
                return false;
            }

            while (ChildrenCount < count)
            {
                var property = _property.ArrayElementProperties[ChildrenCount];
                AddChild(CreateItemElement(property));
            }

            while (ChildrenCount > count)
            {
                RemoveChildAt(ChildrenCount - 1);
            }

            return true;
        }

        private bool ClearChildren()
        {
            if (ChildrenCount == 0)
            {
                return false;
            }

            RemoveAllChildren();

            return true;
        }

        protected virtual TriElement CreateItemElement(TriProperty property)
        {
            #region カスタマイズ: テーブル対応

            if (_table)
            {
                return new TableRowElement(property);
            }

            #endregion

            return new TriPropertyElement(property, new TriPropertyElement.Props
            {
                forceInline = !_showElementLabels,
            });
        }

        private void DrawHeaderCallback(Rect rect)
        {
            var labelRect = new Rect(rect)
            {
                xMax = rect.xMax - 50,
            };

            #region カスタマイズ: テーブル対応

            labelRect.height = 18;

            #endregion

            #region カスタマイズ: 要素数を変更するためのフィールドを描画

            var arraySizeRect = new Rect(rect)
            {
                xMin = labelRect.xMax,
            };

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                var arraySize = EditorGUI.IntField(arraySizeRect, GUIContent.none, _reorderableListGui.count);
                if (changeCheckScope.changed)
                {
                    // Add
                    if (arraySize > _reorderableListGui.count)
                    {
                        for (var i = _reorderableListGui.count; i < arraySize; ++i)
                        {
                            AddElementCallback(_reorderableListGui);
                        }
                    }
                    // Remove
                    else if (arraySize < _reorderableListGui.count)
                    {
                        for (var i = _reorderableListGui.count; i > arraySize; --i)
                        {
                            RemoveElementCallback(_reorderableListGui);
                        }
                    }
                }
            }

            #endregion

            #region カスタマイズ: TSVとの相互変換

            if (_table)
            {
                var fromTsvButtonRect = new Rect(labelRect)
                {
                    xMin = arraySizeRect.xMin - 3 - 36,
                    xMax = arraySizeRect.xMin - 3,
                };
                if (GUI.Button(fromTsvButtonRect, "貼付"))
                {
                    if (_property.TryGetSerializedProperty(out var serializedProperty))
                    {
                        var tsvText = EditorGUIUtility.systemCopyBuffer;
                        TriTsvConverterContext.Converter.TsvTextToSerializedProperty(tsvText, serializedProperty);
                    }
                    else
                    {
                        Debug.LogError("SerializedPropertyの取得に失敗しました");
                    }
                }

                var toTsvButtonRect = new Rect(labelRect)
                {
                    xMin = fromTsvButtonRect.xMin - 3 - 44,
                    xMax = fromTsvButtonRect.xMin - 3,
                };
                if (GUI.Button(toTsvButtonRect, "コピー"))
                {
                    if (_property.TryGetSerializedProperty(out var serializedProperty))
                    {
                        var tsvText = TriTsvConverterContext.Converter.SerializePropertyToTsvText(serializedProperty);
                        EditorGUIUtility.systemCopyBuffer = tsvText;
                    }
                    else
                    {
                        Debug.LogError("SerializedPropertyの取得に失敗しました");
                    }
                }
            }

            #endregion

            if (_alwaysExpanded)
            {
                EditorGUI.LabelField(labelRect, _property.DisplayNameContent);
            }
            else
            {
                TriEditorGUI.Foldout(labelRect, _property);
            }

            #region カスタマイズ: 要素数はラベルとしては表示しない

            // var label = _reorderableListGui.count == 0 ? "Empty" : $"{_reorderableListGui.count} items";
            // GUI.Label(arraySizeRect, label, Styles.ItemsCount);

            #endregion

            if (Event.current.type == EventType.DragUpdated && rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDrop.objectReferences.All(obj => TryGetDragAndDropObject(obj, out _))
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;

                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (TryGetDragAndDropObject(obj, out var addedReferenceValue))
                    {
                        AddElementCallback(_reorderableListGui, addedReferenceValue);
                    }
                }

                Event.current.Use();
            }

            #region カスタマイズ: テーブル対応

            if (_table)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var backgroundRect = new Rect
                    {
                        xMin = rect.xMin - 6,
                        xMax = rect.xMax + 6,
                        yMin = rect.yMin + 19,
                        yMax = rect.yMax + 3,
                    };
                    ReorderableList.defaultBehaviours.boxBackground.Draw(backgroundRect, false, false, false, false);
                }

                if (ChildrenCount != 0)
                {
                    var cellElements = ((TableRowElement) (GetChild(0))).Elements;
                    var headersRect = new Rect(rect)
                    {
                        xMin = rect.xMin + 15,
                        y = rect.y + 20,
                        height = 12,
                        xMax = rect.xMax - 1,
                    };

                    // 幅の割合を維持したまま headersRect.width に合わせる
                    var totalWidth = _columnWidths.Sum();
                    if (totalWidth > 0)
                    {
                        for (var i = 0; i < _columnWidths.Length; i++)
                        {
                            _columnWidths[i] = _columnWidths[i] * headersRect.width / totalWidth;
                        }
                    }
                    else
                    {
                        var defaultColumnWidth = headersRect.width / cellElements.Count;
                        for (var i = 0; i < _columnWidths.Length; i++)
                        {
                            _columnWidths[i] = defaultColumnWidth;
                        }
                    }

                    var left = headersRect.x;
                    for (var i = 0; i < cellElements.Count; i++)
                    {
                        var cellContent = cellElements[i].Value;
                        var cellRect = new Rect(headersRect)
                        {
                            x = left,
                            width = _columnWidths[i],
                        };
                        left += cellRect.width;

                        EditorGUI.LabelField(cellRect, cellContent, EditorStyles.centeredGreyMiniLabel);

                        // 境界線
                        if (i < cellElements.Count - 1)
                        {
                            var color = new Color(1, 1, 1, 0.1f);
                            var delta = DrawDragLine(cellRect.xMax, cellRect.y, cellRect.height, color);
                            if (delta != 0)
                            {
                                var minWidth = 20;
                                if (_columnWidths[i] + delta < minWidth)
                                {
                                    delta = minWidth - _columnWidths[i];
                                }
                                else if (_columnWidths[i + 1] - delta < minWidth)
                                {
                                    delta = _columnWidths[i + 1] - minWidth;
                                }

                                _columnWidths[i] += delta;
                                _columnWidths[i + 1] -= delta;
                            }
                        }
                    }
                }
            }

            #endregion
        }
        
        private void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (_lastInvisibleElement.HasValue && index + 1 < _lastInvisibleElement.Value ||
                _lastVisibleElement.HasValue && index - 1 > _lastVisibleElement.Value)
            {
                if (index != _reorderableListGui.index)
                {
                    return;
                }
            }

            if ((_showAlternatingBackground || _alternatingRowBackgrounds) && index % 2 != 0)
            {
                EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.15f));
            }

            ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused,
                _reorderableListGui.draggable);
        }

        #region カスタマイズ: テーブル対応

        private static float DrawDragLine(float x, float y, float height, Color color)
        {
            var lineRect = new Rect(x - 1, y, 1, height);
            EditorGUI.DrawRect(lineRect, color);

            var draggableRect = new Rect(lineRect)
            {
                xMin = lineRect.xMin - 5,
                xMax = lineRect.xMax + 5,
            };
            EditorGUIUtility.AddCursorRect(draggableRect, MouseCursor.ResizeHorizontal);

            // ドラッグ検出して移動量を返す
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);
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

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= ChildrenCount)
            {
                return;
            }

            if (_lastInvisibleElement.HasValue && index + 1 < _lastInvisibleElement.Value ||
                _lastVisibleElement.HasValue && index - 1 > _lastVisibleElement.Value)
            {
                if (index != _reorderableListGui.index)
                {
                    return;
                }
            }

            if (_reorderableListGui.count > MinElementsForVirtualization)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var windowRect = GUIClipProxy.VisibleRect;
                    var rectInWindow = GUIClipProxy.UnClipToWindow(rect);

                    if (rectInWindow.yMax < 0)
                    {
                        _lastInvisibleElement = index;
                    } else if (_lastInvisibleElement == index)
                    {
                        _lastInvisibleElement = index / 2;
                        _lastVisibleElement = index / 2 + 1;
                        _property.PropertyTree.RequestRepaint();
                    }

                    if (rectInWindow.y < windowRect.height)
                    {
                        if (!_lastVisibleElement.HasValue || index > _lastVisibleElement.Value)
                        {
                            _lastVisibleElement = index;
                        }
                    }
                }
            }

            if (!_reorderableListGui.draggable)
            {
                rect.xMin += DraggableAreaExtraWidth;
            }

            #region カスタマイズ: 上端にスペースを追加

            rect.yMin += 1;

            #endregion

            var indexRect = new Rect(rect)
            {
                x = rect.x - 19,
                width = 19,
                yMin = rect.yMin + 3,
                yMax = rect.yMax - 1,
            };
            EditorGUI.LabelField(indexRect, index.ToString(), Styles.ElementIndex);

            #region カスタマイズ: テーブル対応

            if (_table)
            {
                using (TriPropertyOverrideContext.BeginOverride(_tableListPropertyOverrideContext))
                {
                    var rowElement = (TableRowElement) (GetChild(index));
                    var cellElements = rowElement.Elements;
                    var left = rect.x;
                    for (var i = 0; i < cellElements.Count; i++)
                    {
                        var cellElement = cellElements[i].Key;
                        var width = _columnWidths[i];
                        var cellRect = new Rect(rect)
                        {
                            height = cellElement.GetHeight(width),
                            x = left,
                            width = width - 2,
                        };
                        left += width;

                        using (TriGuiHelper.PushLabelWidth(EditorGUIUtility.labelWidth / rowElement.ChildrenCount))
                        {
                            cellElement.OnGUI(cellRect);
                        }
                    }
                }

                return;
            }

            #endregion

            using (TriPropertyOverrideContext.BeginOverride(ListPropertyOverrideContext.Instance))
            {
                GetChild(index).OnGUI(rect);
            }
        }

        private float ElementHeightCallback(int index)
        {
            #region カスタマイズ: テーブル対応

            if (_table && index < ChildrenCount)
            {
                var height = 0f;
                var rowElement = (TableRowElement) GetChild(index);
                var cellElements = rowElement.Elements;

                if (_columnWidths == null)
                {
                    _columnWidths = new float[cellElements.Count];
                    var defaultColumnWidth = _lastContentWidth / cellElements.Count;
                    for (var i = 0; i < _columnWidths.Length; i++)
                    {
                        _columnWidths[i] = defaultColumnWidth;
                    }
                }

                for (var i = 0; i < cellElements.Count; i++)
                {
                    var cellElement = cellElements[i];
                    var cellWidth = _columnWidths[i] - 2;
                    var cellHeight = cellElement.Key.GetHeight(cellWidth);
                    height = Math.Max(height, cellHeight);
                }

                return height;
            }

            #endregion

            if (index >= ChildrenCount)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (_lastInvisibleElement.HasValue && index + 1 < _lastInvisibleElement.Value ||
                _lastVisibleElement.HasValue && index - 1 > _lastVisibleElement.Value)
            {
                if (index != _reorderableListGui.index)
                {
                    return Mathf.Max(EditorGUIUtility.singleLineHeight, GetChild(index).CachedHeight);
                }
            }

            return GetChild(index).GetHeight(_lastContentWidth);
        }

        private static object CreateDefaultElementValue(TriProperty property)
        {
            var canActivate = property.ArrayElementType.IsValueType ||
                              property.ArrayElementType.GetConstructor(Type.EmptyTypes) != null;

            return canActivate ? Activator.CreateInstance(property.ArrayElementType) : null;
        }

        private static Array CloneValue(TriProperty property)
        {
            var list = (IList) property.Value;
            var template = Array.CreateInstance(property.ArrayElementType, list?.Count ?? 0);
            list?.CopyTo(template, 0);
            return template;
        }

        private bool TryGetDragAndDropObject(Object obj, out Object result)
        {
            if (obj == null)
            {
                result = null;
                return false;
            }

            var elementType = _property.ArrayElementType;
            var objType = obj.GetType();

            if (elementType == objType || elementType.IsAssignableFrom(objType))
            {
                result = obj;
                return true;
            }

            if (obj is GameObject go && typeof(Component).IsAssignableFrom(elementType) &&
                go.TryGetComponent(elementType, out var component))
            {
                result = component;
                return true;
            }

            result = null;
            return false;
        }

        private class ListPropertyOverrideContext : TriPropertyOverrideContext
        {
            public static readonly ListPropertyOverrideContext Instance = new ListPropertyOverrideContext();

            private readonly GUIContent _noneLabel = GUIContent.none;

            public override bool TryGetDisplayName(TriProperty property, out GUIContent displayName)
            {
                #region カスタマイズ: 要素のラベルを任意に指定可能にする

                // var showLabels = property.TryGetAttribute(out ListDrawerSettingsAttribute settings) &&
                //                  settings.ShowElementLabels;
                //
                // if (!showLabels)
                // {
                //     displayName = _noneLabel;
                //     return true;
                // }

                if (property.TryGetAttribute(out ListDrawerSettingsAttribute settings) &&
                    !string.IsNullOrEmpty(settings.ElementLabelMethod))
                {
                    var elementLabelResolver = ValueResolver.Resolve<string, int>(
                        property.Definition, settings.ElementLabelMethod, property.IndexInArray);
                    var label = elementLabelResolver.GetValue(property, property.IndexInArray);
                    if (!string.IsNullOrEmpty(label))
                    {
                        displayName = new GUIContent(label);
                        return true;
                    }
                }

                #endregion

                #region カスタマイズ: ToStringメソッドがoverrideされている場合、リスト要素のラベルとして利用する

                if (IsToStringMethodOverridden(property) && property.Value != null)
                {
                    displayName = new GUIContent(property.Value.ToString());
                    return true;
                }

                #endregion

                #region カスタマイズ: 折りたたみできる要素の場合は「…」を表示する

                if (property.PropertyType != TriPropertyType.Primitive)
                {
                    displayName = new GUIContent("\u2026");
                    return true;
                }

                #endregion

                displayName = _noneLabel;
                return true;
            }
        }

        #region カスタマイズ: ToStringメソッドがoverrideされている場合、リスト要素のラベルとして利用する

        private static bool IsToStringMethodOverridden(TriProperty property)
        {
            var method = property.FieldType.GetMethod("ToString", Type.EmptyTypes);

            if (method == null) return false;
            if (method.DeclaringType == null) return false;
            if (method.DeclaringType == method.GetBaseDefinition().DeclaringType) return false;

            // UnityEngine名前空間の型と組み込み型はToStringをオーバーライドしていても無視する
            if (method.DeclaringType.Namespace == null) return true;
            var nameSpaceHead = method.DeclaringType.Namespace.Split('.')[0];
            if (nameSpaceHead == "UnityEngine") return false;
            if (nameSpaceHead == "UnityEditor") return false;
            if (nameSpaceHead == "System") return false;

            return true;
        }

        #endregion

        private static class Styles
        {
            public static readonly GUIStyle ItemsCount;

            #region カスタマイズ: テーブル対応

            public static readonly GUIStyle ElementIndex;

            #endregion

            static Styles()
            {
                ItemsCount = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin
                            ? new Color(0.6f, 0.6f, 0.6f)
                            : new Color(0.3f, 0.3f, 0.3f),
                    },
                };

                #region カスタマイズ: テーブル対応

                ElementIndex = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 9,
                };

                #endregion
            }
        }

        #region カスタマイズ: テーブル対応

        private class TableRowElement : TriPropertyCollectionBaseElement
        {
            public TableRowElement(TriProperty property)
            {
                DeclareGroups(property.ValueType);

                Elements = new List<KeyValuePair<TriElement, GUIContent>>();

                if (property.PropertyType == TriPropertyType.Generic)
                {
                    foreach (var childProperty in property.ChildrenProperties)
                    {
                        var oldChildrenCount = ChildrenCount;

                        var props = new TriPropertyElement.Props
                        {
                            forceInline = true,
                        };
                        AddProperty(childProperty, props, out var group);

                        if (oldChildrenCount != ChildrenCount)
                        {
                            var element = GetChild(ChildrenCount - 1);
                            var headerContent = new GUIContent(group ?? childProperty.DisplayName);

                            Elements.Add(new KeyValuePair<TriElement, GUIContent>(element, headerContent));
                        }
                    }
                }
                else
                {
                    var element = new TriPropertyElement(property, new TriPropertyElement.Props
                    {
                        forceInline = true,
                    });
                    var headerContent = new GUIContent("Element");

                    AddChild(element);
                    Elements.Add(new KeyValuePair<TriElement, GUIContent>(element, headerContent));
                }
            }

            public List<KeyValuePair<TriElement, GUIContent>> Elements { get; }
        }

        private class TableListPropertyOverrideContext : TriPropertyOverrideContext
        {
            private readonly TriProperty _grandParentProperty;
            private readonly GUIContent _noneLabel = GUIContent.none;

            public TableListPropertyOverrideContext(TriProperty grandParentProperty)
            {
                _grandParentProperty = grandParentProperty;
            }

            public override bool TryGetDisplayName(TriProperty property, out GUIContent displayName)
            {
                if (property.PropertyType == TriPropertyType.Primitive &&
                    property.Parent?.Parent == _grandParentProperty &&
                    !property.TryGetAttribute(out GroupAttribute _))
                {
                    displayName = _noneLabel;
                    return true;
                }

                displayName = default;
                return false;
            }
        }

        #endregion
    }
}