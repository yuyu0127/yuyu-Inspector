using System;
using System.Reflection;
using UnityEngine;

namespace TriInspector.Resolvers
{
    // カスタマイズ: 引数を取れるようにする
    internal sealed class InstanceMethodValueResolver<T, TArg> : ValueResolver<T, TArg>
    {
        private readonly MethodInfo _methodInfo;

        public static bool TryResolve(TriPropertyDefinition propertyDefinition, string expression, TArg arg,
            out ValueResolver<T, TArg> resolver)
        {
            var parentType = propertyDefinition.OwnerType;
            if (parentType == null)
            {
                resolver = null;
                return false;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var methodInfo in parentType.GetMethods(flags))
            {
                if (methodInfo.Name == expression &&
                    typeof(T).IsAssignableFrom(methodInfo.ReturnType) &&
                    methodInfo.GetParameters() is var parameterInfos &&
                    parameterInfos.Length == 1 &&
                    typeof(TArg).IsAssignableFrom(parameterInfos[0].ParameterType))
                {
                    resolver = new InstanceMethodValueResolver<T, TArg>(methodInfo);
                    return true;
                }
            }

            resolver = null;
            return false;
        }

        private InstanceMethodValueResolver(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        public override bool TryGetErrorString(out string error, TArg arg)
        {
            error = "";
            return false;
        }

        public override T GetValue(TriProperty property, TArg arg, T defaultValue = default)
        {
            var parentValue = property.Owner.GetValue(0);

            try
            {
                return (T) _methodInfo.Invoke(parentValue, new object[] {arg});
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException targetInvocationException)
                {
                    e = targetInvocationException.InnerException;
                }

                Debug.LogException(e);
                return defaultValue;
            }
        }
    }
}