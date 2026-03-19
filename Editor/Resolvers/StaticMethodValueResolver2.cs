using System;
using System.Reflection;
using TriInspector.Utilities;
using UnityEngine;

namespace TriInspector.Resolvers
{
    // カスタマイズ: 引数を取れるようにする
    public class StaticMethodValueResolver<T, TArg> : ValueResolver<T, TArg>
    {
        private readonly MethodInfo _methodInfo;

        public static bool TryResolve(TriPropertyDefinition propertyDefinition, string expression, TArg arg,
            out ValueResolver<T, TArg> resolver)
        {
            var type = propertyDefinition.OwnerType;
            var methodName = expression;

            var separatorIndex = expression.LastIndexOf('.');
            if (separatorIndex >= 0)
            {
                var className = expression.Substring(0, separatorIndex);
                methodName = expression.Substring(separatorIndex + 1);

                if (!TriReflectionUtilities.TryFindTypeByFullName(className, out type))
                {
                    resolver = null;
                    return false;
                }
            }

            if (type == null)
            {
                resolver = null;
                return false;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var methodInfo in type.GetMethods(flags))
            {
                if (methodInfo.Name == methodName &&
                    typeof(T).IsAssignableFrom(methodInfo.ReturnType) &&
                    methodInfo.GetParameters() is var parametersInfo &&
                    parametersInfo.Length == 1 &&
                    typeof(TArg).IsAssignableFrom(parametersInfo[0].ParameterType))
                {
                    resolver = new StaticMethodValueResolver<T, TArg>(methodInfo);
                    return true;
                }
            }

            resolver = null;
            return false;
        }

        public StaticMethodValueResolver(MethodInfo methodInfo)
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
            try
            {
                return (T) _methodInfo.Invoke(null, new object[] {arg});
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