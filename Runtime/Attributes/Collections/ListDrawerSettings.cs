using System;
using System.Diagnostics;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class ListDrawerSettingsAttribute : Attribute
    {
        public bool Draggable { get; set; } = true;
        public bool HideAddButton { get; set; }
        public bool HideRemoveButton { get; set; }
        public bool AlwaysExpanded { get; set; }
        public bool ShowElementLabels { get; set; }
        public bool ShowDefaultBackground { get; set; } = true;
        public bool ShowAlternatingBackground { get; set; } = true;

        #region カスタマイズ: 要素のラベルを任意に指定可能にする

        public string ElementLabelMethod { get; set; }

        #endregion

        #region カスタマイズ: テーブル対応

        public bool Table { get; set; }

        #endregion

        #region カスタマイズ: 交互の背景色

        public bool AlternatingRowBackgrounds { get; set; }

        #endregion
    }
}