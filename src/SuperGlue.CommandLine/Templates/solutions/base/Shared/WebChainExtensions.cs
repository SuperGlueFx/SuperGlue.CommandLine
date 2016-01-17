using System.Collections.Generic;
using SuperGlue.Configuration;

namespace SuperGlue
{
    internal static class WebChainExtensions
    {
        public static string GetWebApplicationRoot(this IDictionary<string, object> environment)
        {
            var settings = environment.GetChainSettings("chains.Web");

            return settings.GetSetting("root", "/");
        }

        public static ChainSettings SetWebApplicationRoot(this ChainSettings settings, string root)
        {
            return settings.UseSetting("root", root);
        }
    }
}