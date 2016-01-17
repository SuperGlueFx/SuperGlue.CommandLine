using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperGlue.Hosting
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class RunAppFunc
    {
        private readonly AppFunc _next;
        private readonly RunAppFuncOptions _options;

        public RunAppFunc(AppFunc next, RunAppFuncOptions options)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _next = next;
            _options = options;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            await _options.Func(environment).ConfigureAwait(false);

            await _next(environment).ConfigureAwait(false);
        }
    }

    public class RunAppFuncOptions
    {
        public RunAppFuncOptions(AppFunc func)
        {
            Func = func;
        }

        public AppFunc Func { get; }
    }
}