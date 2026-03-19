using System;
using System.Diagnostics;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class DropdownAttribute : Attribute
    {
        public string Values { get; }

        public TriMessageType ValidationMessageType { get; set; } = TriMessageType.Error;
        public bool Advanced { get; set; } = true;

        public DropdownAttribute(string values)
        {
            Values = values;
        }

        #region カスタマイズ: ドロップダウンを表示する条件を指定可能にする

        public string Condition { get; }

        public DropdownAttribute(string values, string condition)
        {
            Values = values;
            Condition = condition;
        }

        #endregion
    }
}