using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SuperGlue
{
    internal static class LogExtensions
    {
        private static readonly ConcurrentStack<Tuple<Exception, string, string, object[]>> LogMessagesToAdd = new ConcurrentStack<Tuple<Exception, string, string, object[]>>();

        public static class LogConstants
        {
            public const string WriteToLogFunction = "superglue.WriteToLogFunction";
        }

        public static void Log(this IDictionary<string, object> environment, string message, string logLevel, params object[] parameters)
        {
            Log(environment, null, message, logLevel, parameters);
        }

        public static void Log(this IDictionary<string, object> environment, Exception exception, string message, string logLevel, params object[] parameters)
        {
            var log = environment.Get<Action<IDictionary<string, object>, Exception, string, string, object[]>>(LogConstants.WriteToLogFunction);

            if (log == null)
            {
                if (LogMessagesToAdd.Count < 1000)
                    LogMessagesToAdd.Push(new Tuple<Exception, string, string, object[]>(exception, message, logLevel, parameters));

                return;
            }

            while (LogMessagesToAdd.Count > 0)
            {
                Tuple<Exception, string, string, object[]> item;

                if(!LogMessagesToAdd.TryPop(out item))
                    break;

                log(environment, item.Item1, item.Item2, item.Item3, item.Item4);
            }

            log(environment, exception, message, logLevel, parameters);
        }
    }

    internal static class LogLevel
    {
        public const string Debug = "Debug";
        public const string Info = "Info";
        public const string Warn = "Warn";
        public const string Error = "Error";
        public const string Fatal = "Fatal";
    }
}