#region カスタマイズ: TSVとの相互変換

using UnityEditor;

namespace TriInspector
{
    public static class TriTsvConverterContext
    {
        public static ITriTsvConverter Converter { get; set; }
    }

    public interface ITriTsvConverter
    {
        string SerializePropertyToTsvText(SerializedProperty arrayProperty);
        void TsvTextToSerializedProperty(string tsvText, SerializedProperty arrayProperty);
    }
}

#endregion