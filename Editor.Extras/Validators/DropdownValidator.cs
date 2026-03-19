using TriInspector;
using TriInspector.Resolvers;
using TriInspector.Validators;

[assembly: RegisterTriAttributeValidator(typeof(DropdownValidator<>), ApplyOnArrayElement = true)]

namespace TriInspector.Validators
{
    public class DropdownValidator<T> : TriAttributeValidator<DropdownAttribute>
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

        public override TriValidationResult Validate(TriProperty property)
        {
            #region カスタマイズ: ドロップダウンを表示する条件を指定可能にする

            if (_conditionResolver != null && !_conditionResolver.GetValue(property, true))
            {
                return TriValidationResult.Valid;
            }

            #endregion

            foreach (var item in _valuesResolver.GetDropdownItems(property))
            {
                if (property.Comparer.Equals(item.Value, property.Value))
                {
                    return TriValidationResult.Valid;
                }
            }

            var msg = $"Dropdown value '{property.Value}' not valid";

            switch (Attribute.ValidationMessageType)
            {
                case TriMessageType.Info:
                    return TriValidationResult.Info(msg);

                case TriMessageType.Warning:
                    return TriValidationResult.Warning(msg);

                case TriMessageType.Error:
                    return TriValidationResult.Error(msg);
            }

            return TriValidationResult.Valid;
        }
    }
}