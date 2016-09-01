using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SuperGlue
{
    public class RunCommand : ICommand
    {
        public RunCommand()
        {
            Hosts = new List<string>();
            IgnoredPaths = new List<string>();
        }

        public string Application { get; set; }
        public string Config { get; set; }
        public string Environment { get; set; }
        public ICollection<string> Hosts { get; set; }
        public ICollection<string> IgnoredPaths { get; set; }

        public async Task Execute()
        {
            var configuration = GetConfiguration();

            var applications = GetApplications(configuration).ToList();

            Console.CancelKeyPress += async (x, y) => await StopApplications(applications).ConfigureAwait(false);

            await ExecuteApplications(applications, x => x.Start(), (app, exception) => app.Stop()).ConfigureAwait(false);

            var key = Console.ReadKey();

            while (key.Key != ConsoleKey.Q)
            {
                if (key.Key != ConsoleKey.R)
                    continue;

                Console.WriteLine();

                await ExecuteApplications(applications, x => x.Recycle(), (app, exception) => app.Stop()).ConfigureAwait(false);

                key = Console.ReadKey();
            }

            await StopApplications(applications).ConfigureAwait(false);
        }

        private RunConfiguration GetConfiguration()
        {
            if (!string.IsNullOrEmpty(Config) && File.Exists(Config))
            {
                var config = JsonConvert.DeserializeObject<RunConfiguration>(File.ReadAllText(Config));

                if (string.IsNullOrEmpty(config.Application))
                    config.Application = Application;

                if (string.IsNullOrEmpty(config.Environment))
                    config.Environment = Environment;

                if (config.Hosts == null || !config.Hosts.Any())
                {
                    config.Hosts = Hosts.Select(x => new RunConfiguration.HostConfiguration
                    {
                        Name = x,
                        Arguments = new List<string>()
                    }).ToList();
                }

                if (config.IgnoredPaths == null || !config.IgnoredPaths.Any())
                    config.IgnoredPaths = IgnoredPaths.ToList();

                return config;
            }

            return new RunConfiguration
            {
                IgnoredPaths = IgnoredPaths.ToList(),
                Application = Application,
                Hosts = Hosts.Select(x => new RunConfiguration.HostConfiguration
                {
                    Name = x,
                    Arguments = new List<string>()
                }).ToList(),
                Environment = Environment
            };
        }

        private static async Task StopApplications(IEnumerable<RunnableApplication> applications)
        {
            await ExecuteApplications(applications, x => x.Stop()).ConfigureAwait(false);
        }

        private static async Task ExecuteApplications(IEnumerable<RunnableApplication> applications,
            Func<RunnableApplication, Task> execute, Func<RunnableApplication, Exception, Task> onError = null)
        {
            foreach (var application in applications)
            {
                try
                {
                    await execute(application).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (onError != null)
                        await onError(application, ex).ConfigureAwait(false);

                    Console.WriteLine($"Application {application.ApplicationName} failed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private static IEnumerable<RunnableApplication> GetApplications(RunConfiguration runConfiguration)
        {
            var hostArguments = runConfiguration.Hosts.ToDictionary(x => x.Name, x => (x.Arguments ?? new List<string>()).ToArray());

            yield return CreateApplication(runConfiguration, hostArguments);
        }

        private static RunnableApplication CreateApplication(RunConfiguration runConfiguration, IDictionary<string, string[]> hostArguments)
        {
            var applicationName = GetApplicationName(runConfiguration.Application);

            return new RunnableApplication(runConfiguration.Environment, runConfiguration.Application,
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                    $"Applications\\{applicationName}"), applicationName,
                runConfiguration.Hosts.Select(x => new ApplicationHost(x.Name)).ToList(), hostArguments, (runConfiguration.IgnoredPaths ?? new List<string>()).ToArray());
        }

        private static string GetApplicationName(string path)
        {
            var invalidPaths = new List<string>
            {
                "bin",
                "obj",
                "debug",
                "release"
            };

            var currentPath = path;
            var name = Path.GetFileName(currentPath);

            while (true)
            {
                if (!invalidPaths.Any(x => x.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                    return name;

                currentPath = Path.GetDirectoryName(currentPath);

                name = Path.GetFileName(currentPath);

                if (string.IsNullOrEmpty(currentPath) || string.IsNullOrEmpty(name))
                    return "Default";
            }
        }

        public class RunConfiguration
        {
            public string Application { get; set; }
            public string Environment { get; set; }
            public List<HostConfiguration> Hosts { get; set; }
            public List<string> IgnoredPaths { get; set; }

            public class HostConfiguration
            {
                public string Name { get; set; }
                public IEnumerable<string> Arguments { get; set; }
            }
        }
    }
}