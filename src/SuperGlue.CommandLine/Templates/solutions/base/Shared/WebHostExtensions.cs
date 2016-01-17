using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperGlue
{
    internal static class WebHostExtensions
    {
        public class WebHostConstants
        {
            public const string Bindings = "superglue.WebHost.Bindings";
        }

        public static IEnumerable<string> GetWebBindings(this IDictionary<string, object> environment)
        {
            return environment.Get<Func<IEnumerable<string>>>(WebHostConstants.Bindings, Enumerable.Empty<string>)();
        }
    }
}