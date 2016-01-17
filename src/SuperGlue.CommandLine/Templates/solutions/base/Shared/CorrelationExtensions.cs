using System;
using System.Collections.Generic;

namespace SuperGlue
{
    internal static class CorrelationExtensions
    {
        public class CorrelationConstants
        {
            public const string CausationId = "superglue.CausationId";
            public const string CorrelationId = "superglue.CorrelationId";
        }

        public static IDisposable OpenCorrelationContext(this IDictionary<string, object> environment, string correlationId)
        {
            return new CausationContext(environment, CorrelationConstants.CorrelationId, correlationId);
        }

        public static IDisposable OpenCausationContext(this IDictionary<string, object> environment, string causationId)
        {
            return new CausationContext(environment, CorrelationConstants.CausationId, causationId);
        }

        public static string GetCorrelationId(this IDictionary<string, object> environment)
        {
            return environment.Get<string>(CorrelationConstants.CorrelationId);
        }

        public static string GetCausationId(this IDictionary<string, object> environment)
        {
            return environment.Get<string>(CorrelationConstants.CausationId);
        }

        private class CausationContext : IDisposable
        {
            private readonly IDictionary<string, object> _environment;
            private readonly string _key;
            private readonly string _oldValue;

            public CausationContext(IDictionary<string, object> environment, string key, string newValue)
            {
                _environment = environment;
                _key = key;
                _oldValue = environment.Get<string>(key);
                environment[key] = newValue;
            }

            public void Dispose()
            {
                _environment[_key] = _oldValue;
            }
        }
    }
}