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
        }

        public string Application { get; set; }
        public string ConfigFile { get; set; }
        public string Environment { get; set; }
        public ICollection<string> Hosts { get; set; }

        public async Task Execute()
        {
            var applications = GetApplications().ToList();

            await ExecuteApplications(applications, x => x.Start(), (app, exception) => app.Stop());

            var key = Console.ReadKey();

            while (key.Key != ConsoleKey.Q)
            {
                if (key.Key != ConsoleKey.R)
                    continue;

                Console.WriteLine();

                await ExecuteApplications(applications, x => x.Recycle(), (app, exception) => app.Stop());

                key = Console.ReadKey();
            }

            await ExecuteApplications(applications, x => x.Stop());
        }

        private static async Task ExecuteApplications(IEnumerable<RunnableApplication> applications,
            Func<RunnableApplication, Task> execute, Func<RunnableApplication, Exception, Task> onError = null)
        {
            foreach (var application in applications)
            {
                try
                {
                    await execute(application);
                }
                catch (Exception ex)
                {
                    if (onError != null)
                        await onError(application, ex);

                    Console.WriteLine($"Application {application.GetType().Name} failed: {ex.Message}");
                }
            }
        }

        private IEnumerable<RunnableApplication> GetApplications()
        {
            if (string.IsNullOrEmpty(ConfigFile))
            {
                yield return CreateApplication(Application, Hosts);

                yield break;
            }

            if (!File.Exists(ConfigFile))
                yield break;

            var configuration =
                JsonConvert.DeserializeObject<IEnumerable<ApplicationsConfig>>(File.ReadAllText(ConfigFile));

            foreach (var application in configuration)
                yield return CreateApplication(application.Path, application.Hosts ?? new List<string>());
        }

        private RunnableApplication CreateApplication(string application, IEnumerable<string> hosts)
        {
            var applicationName = GetApplicationName(application);

            return new RunnableApplication(Environment, application,
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                    $"Applications\\{applicationName}"), applicationName,
                hosts.Select(x => new ApplicationHost(x)).ToList());
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

        public class ApplicationsConfig
        {
            public string Path { get; set; }
            public IEnumerable<string> Hosts { get; set; }
        }
    }
}