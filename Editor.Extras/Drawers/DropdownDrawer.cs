using TriInspector;
using TriInspector.Drawers;
using TriInspector.Elements;
using TriInspector.Resolvers;
using UnityEngine;

[assembly: RegisterTriAttributeDrawer(typeof(DropdownDrawer<>), TriDrawerOrder.Decorator, ApplyOnArrayElement = true)]

namespace TriInspector.Drawers
{
    public class DropdownDrawer<T> : TriAttributeDrawer<DropdownAttribute>
    {
        private DropdownValuesResolver<T> _valuesResolver;

        #region カスタマイズ: ドロップダウンを表示する条件を指定可能にする

        private ValueResolver<bool> _conditionResolver;

        #endregion

        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            _valuesResolver = DropdownValuesResolver<T>.Resolve(propertyDefinition, Attribute.Values);

            if (_valuesResolver.TryGetErrorString(out var error))
            {
                return error;
            }

            #region カスタマイズ: ドロップダウンを表示する条件を指定可能にする

            if (Attribute.Condition != null)
            {
                _conditionResolver = ValueResolver.Resolve<bool>(propertyDefinition, Attribute.Condition);

                if (_conditionResolver.TryGetErrorString(out error))
                {
                    return error;
                }
            }

            #endregion

            return TriExtensionInitializationResult.Ok;
        }

        public override TriElement CreateElement(TriProperty property, TriElement next)
        {
            #region カスタマイズ: ドロップダウンを表示する条件を指定可能にする

            if (_conditionResolver != null && !_conditionResolver.GetValue(property, true))
            {
                return base.CreateElement(property, next);
            }

            #endregion

            return new TriDropdownElement(property, _valuesResolver.GetDropdownItems, Attribute.Advanced);
        }
    }
}