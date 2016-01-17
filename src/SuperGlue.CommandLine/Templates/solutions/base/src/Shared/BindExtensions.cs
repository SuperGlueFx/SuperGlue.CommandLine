using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperGlue
{
    internal static class BindExtensions
    {
        public static class BindConstants
        {
            public const string ModelBinder = "superglue.ModelBinder";
            public const string Output = "superglue.Output";
        }

        public static async Task<T> Bind<T>(this IDictionary<string, object> environment)
        {
            return (T) (await environment.Bind(typeof (T)).ConfigureAwait(false));
        }

        public static async Task<object> Bind(this IDictionary<string, object> environment, Type type)
        {
            var modelBinder = environment.Get<Func<Type, Task<object>>>(BindConstants.ModelBinder);
            var requestTypedParameters = GetRequestTypedParameters(environment);

            return requestTypedParameters.ContainsKey(type) ? requestTypedParameters[type] : await modelBinder(type).ConfigureAwait(false);
        }

        public static void Set<T>(this IDictionary<string, object> environment, T data)
        {
            environment.Set(typeof (T), data);
        }

        public static void Set(this IDictionary<string, object> environment, Type dataType, object data)
        {
            var requestTypedParameters = GetRequestTypedParameters(environment);

            requestTypedParameters[dataType] = data;
        }

        public static void SetOutput<T>(this IDictionary<string, object> environment, T data)
        {
            environment.Set(data);
            environment[BindConstants.Output] = data;
        }

        public static void SetOutput(this IDictionary<string, object> environment, object data)
        {
            environment.Set(data.GetType(), data);
            environment[BindConstants.Output] = data;
        }

        public static object GetOutput(this IDictionary<string, object> environment)
        {
            return environment.Get<object>(BindConstants.Output);
        }

        private static IDictionary<Type, object> GetRequestTypedParameters(IDictionary<string, object> environment)
        {
            var requestTypedParameters = environment.Get<IDictionary<Type, object>>("superglue.RequestTypedParameters");

            if (requestTypedParameters != null) return requestTypedParameters;

            requestTypedParameters = new Dictionary<Type, object>();
            environment["superglue.RequestTypedParameters"] = requestTypedParameters;

            return requestTypedParameters;
        }
    }
}