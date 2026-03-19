namespace TriInspector.Resolvers
{
    // カスタマイズ: 引数を取れるようにする
    internal class ErrorValueResolver<T, TArg> : ValueResolver<T, TArg>
    {
        private readonly string _expression;

        public ErrorValueResolver(TriPropertyDefinition propertyDefinition, string expression)
        {
            _expression = expression;
        }

        public override bool TryGetErrorString(out string error, TArg arg)
        {
            error = $"Method '{_expression}' not exists or has wrong signature";
            return true;
        }

        public override T GetValue(TriProperty property, TArg arg, T defaultValue = default)
        {
            return defaultValue;
        }
    }
}