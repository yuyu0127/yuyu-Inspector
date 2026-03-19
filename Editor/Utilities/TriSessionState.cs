#region カスタマイズ: ラベル幅を調整可能にする

namespace TriInspector.Utilities
{
    internal static class TriSessionState
    {
        private const string LabelWidthKey = "TriInspector.LabelWidth";

        public static float LabelWidth
        {
            get
            {
                var value = UnityEditor.SessionState.GetFloat(LabelWidthKey, 0);
                if (float.IsNaN(value))
                {
                    value = 0;
                }

                return value;
            }
            set => UnityEditor.SessionState.SetFloat(LabelWidthKey, value);
        }
    }
}

#endregion