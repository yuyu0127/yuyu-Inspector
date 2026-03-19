using JetBrains.Annotations;

namespace TriInspector.Resolvers
{
    // カスタマイズ: 引数を取れるようにする
    public static partial class ValueResolver
    {
        public static ValueResolver<T, TArg> Resolve<T, TArg>(TriPropertyDefinition propertyDefinition,
            string expression, TArg arg)
        {
            if (expression != null && expression.StartsWith("$"))
            {
                expression = expression.Substring(1);
            }

            if (StaticMethodValueResolver<T, TArg>.TryResolve(propertyDefinition, expression, arg, out var smr))
            {
                return smr;
            }

            if (InstanceMethodValueResolver<T, TArg>.TryResolve(propertyDefinition, expression, arg, out var imr))
            {
                return imr;
            }

            return new ErrorValueResolver<T, TArg>(propertyDefinition, expression);
        }

        public static ValueResolver<string, TArg> ResolveString<TArg>(TriPropertyDefinition propertyDefinition,
            string expression, TArg arg)
        {
            if (expression != null && expression.StartsWith("$"))
            {
                return Resolve<string, TArg>(propertyDefinition, expression.Substring(1), arg);
            }

            return new ConstantValueResolver<string, TArg>(expression);
        }

        public static bool TryGetErrorString<T, TArg>([CanBeNull] ValueResolver<T, TArg> resolver, out string error,
            TArg arg)
        {
            return TryGetErrorString<T, T, TArg>(resolver, null, out error, arg);
        }

        public static bool TryGetErrorString<T1, T2, TArg>(ValueResolver<T1, TArg> resolver1,
            ValueResolver<T2, TArg> resolver2, out string error, TArg arg)
        {
            if (resolver1 != null && resolver1.TryGetErrorString(out var error1, arg))
            {
                error = error1;
                return true;
            }

            if (resolver2 != null && resolver2.TryGetErrorString(out var error2, arg))
            {
                error = error2;
                return true;
            }

            error = null;
            return false;
        }
    }

    public abstract class ValueResolver<T, TArg>
    {
        [PublicAPI]
        public abstract bool TryGetErrorString(out string error, TArg arg);

        [PublicAPI]
        public abstract T GetValue(TriProperty property, TArg arg, T defaultValue = default);
    }

    public sealed class ConstantValueResolver<T, TArg> : ValueResolver<T, TArg>
    {
        private readonly T _value;

        public ConstantValueResolver(T value)
        {
            _value = value;
        }

        public override bool TryGetErrorString(out string error, TArg arg)
        {
            error = default;
            return false;
        }

        public override T GetValue(TriProperty property, TArg arg, T defaultValue = default)
        {
            return _value;
        }
    }
}