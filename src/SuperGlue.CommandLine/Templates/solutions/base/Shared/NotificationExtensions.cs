using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperGlue
{
    internal static class NotificationExtensions
    {
        public static class NotificationConstants
        {
            public const string NotifyError = "superglue.NotifyError";
        }

        public static NotificationOptions Notifications(this IDictionary<string, object> environment)
        {
            return new NotificationOptions(environment);
        }

        public class NotificationOptions
        {
            private readonly IDictionary<string, object> _environment;

            public NotificationOptions(IDictionary<string, object> environment)
            {
                _environment = environment;
            }

            public Task Error(string from, string message, Exception exception = null)
            {
                return _environment.Get<Func<string, string, IDictionary<string, object>, Exception, Task>>(NotificationConstants.NotifyError, (x, y, z, a) => Task.CompletedTask)(from, message, _environment, exception);
            }
        }
    }
}