using System;
using System.Collections.Generic;

namespace TriInspector.Utilities
{
    public static class TriTypeUtilities
    {
        private static readonly Dictionary<Type, string> TypeNiceNames = new Dictionary<Type, string>();

        public static string GetTypeNiceName(Type type)
        {
            if (TypeNiceNames.TryGetValue(type, out var niceName))
            {
                return niceName;
            }

            #region カスタマイズ: DisplayName属性があれば型名の代わりに利用する

            var displayNameAttribute = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>(type);
            if (displayNameAttribute != null)
            {
                niceName = displayNameAttribute.DisplayName;
                TypeNiceNames[type] = niceName;
                return niceName;
            }

            #endregion

            #region カスタマイズ: 型名表示でスペースを入れる

            // niceName = type.Name;
            niceName = UnityEditor.ObjectNames.NicifyVariableName(type.Name);

            #endregion

            while (type.DeclaringType != null)
            {
                #region カスタマイズ: 型名表示でスペースを入れる

                // niceName = type.DeclaringType.Name + "." + niceName;
                niceName = UnityEditor.ObjectNames.NicifyVariableName(type.DeclaringType.Name) + "." + niceName;

                #endregion

                type = type.DeclaringType;
            }

            #region カスタマイズ: Description属性に対応

            var descriptionAttribute = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(type);
            if (descriptionAttribute != null)
            {
                niceName += " (" + descriptionAttribute.Description + ")";
            }

            #endregion

            TypeNiceNames[type] = niceName;

            return niceName;
        }
    }
}