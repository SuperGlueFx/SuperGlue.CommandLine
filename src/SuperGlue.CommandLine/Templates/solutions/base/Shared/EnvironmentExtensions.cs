using System.Collections.Generic;

namespace SuperGlue
{
    internal static class EnvironmentExtensions
    {
        public static T Get<T>(this IDictionary<string, object> environment, string key, T fallback = default(T))
        {
            object obj;
            if (!environment.TryGetValue(key, out obj) || !(obj is T))
                return fallback;

            return (T)obj;
        }
    }
}