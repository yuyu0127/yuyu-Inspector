#if FALSE // カスタマイズ: ListDrawerSettings側でのテーブル対応
using System;
using System.Diagnostics;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class TableListAttribute : ListDrawerSettingsAttribute
    {
    }
}
#endif