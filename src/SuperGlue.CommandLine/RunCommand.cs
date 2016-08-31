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
                    Application = Application;

                if (string.IsNullOrEmpty(config.Environment))
                    config.Environment = Environment;

                if (!config.Hosts.Any())
                    config.Hosts = Hosts;

                if (!config.IgnoredPaths.Any())
                    config.IgnoredPaths = IgnoredPaths;

                return config;
            }

            return new RunConfiguration(Application, Environment, Hosts, IgnoredPaths);
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

        private IEnumerable<RunnableApplication> GetApplications(RunConfiguration runConfiguration)
        {
            yield return CreateApplication(runConfiguration);
        }

        private RunnableApplication CreateApplication(RunConfiguration runConfiguration)
        {
            var applicationName = GetApplicationName(runConfiguration.Application);

            return new RunnableApplication(runConfiguration.Environment, runConfiguration.Application,
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                    $"Applications\\{applicationName}"), applicationName,
                runConfiguration.Hosts.Select(x => new ApplicationHost(x)).ToList(), (runConfiguration.IgnoredPaths ?? new List<string>()).ToArray());
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
            public RunConfiguration()
            {

            }

            public RunConfiguration(string application, string environment, IEnumerable<string> hosts, IEnumerable<string> ignoredPaths)
            {
                Application = application;
                Environment = environment;
                Hosts = hosts;
                IgnoredPaths = ignoredPaths;
            }

            [JsonProperty("application")]
            public string Application { get; set; }
            [JsonProperty("environment")]
            public string Environment { get; set; }
            [JsonProperty("hosts")]
            public IEnumerable<string> Hosts { get; set; }
            [JsonProperty("ignoredPaths")]
            public IEnumerable<string> IgnoredPaths { get; set; }

            public class HostConfiguration
            {
                public HostConfiguration()
                {

                }

                public HostConfiguration(string name, IEnumerable<string> arguments)
                {
                    Name = name;
                    Arguments = arguments;
                }

                [JsonProperty("name")]
                public string Name { get; set; }
                [JsonProperty("arguments")]
                public IEnumerable<string> Arguments { get; set; }
            }
        }
    }
}