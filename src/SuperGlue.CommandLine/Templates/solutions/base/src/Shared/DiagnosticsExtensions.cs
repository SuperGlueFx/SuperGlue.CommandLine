using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SuperGlue.Configuration;

namespace SuperGlue
{
    internal static class DiagnosticsExtensions
    {
        public class DiagnosticsConstants
        {
            public static string AddData = "superglue.Diagnostics.AddData";
        }

        public static Task PushDiagnosticsData(this IDictionary<string, object> environment, string category, string type, string step, Tuple<string, IDictionary<string, object>> data)
        {
            return environment.Get(DiagnosticsConstants.AddData, (Func<IDictionary<string, object>, string, string, string, Tuple<string, IDictionary<string, object>>, Task>)((x, y, z, a, b) => Task.CompletedTask))(environment, category, type, step, data);
        }
    }

    internal static class DiagnosticsCategories
    {
        public const string Setup = "Setup";

        public static string RequestsFor(IDictionary<string, object> environment)
        {
            return $"{environment.GetCurrentChain().Name}-requests";
        }
    }

    internal static class DiagnosticsTypes
    {
        public const string RequestExecution = "RequestExecution";
        public const string Bootstrapping = "Bootstrapping";
    }
}