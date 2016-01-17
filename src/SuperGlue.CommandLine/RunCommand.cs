using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

            await Task.WhenAll(applications.Select(x => x.Start())).ConfigureAwait(false);

            var key = Console.ReadKey();

            while (key.Key != ConsoleKey.Q)
            {
                if (key.Key != ConsoleKey.R)
                    continue;

                Console.WriteLine();

                await Task.WhenAll(applications.Select(x => x.Recycle())).ConfigureAwait(false);

                key = Console.ReadKey();
            }

            await Task.WhenAll(applications.Select(x => x.Stop())).ConfigureAwait(false);
        }

        private static string ToRepeatableFolderName(string input)
        {
            var hasher = new SHA1Managed();
            var bytes = Encoding.ASCII.GetBytes(input);
            var hash = hasher.ComputeHash(bytes);

            return Regex.Replace(Convert.ToBase64String(hash), @"[^a-zÂ‰ˆ¯ÊA-Z≈ƒ÷ÿ∆0-9!@#\-]+", "");
        }

        private IEnumerable<RunnableApplication> GetApplications()
        {
            if (string.IsNullOrEmpty(ConfigFile))
            {
                yield return CreateApplication(Application, Hosts);

                yield break;
            }

            if(!File.Exists(ConfigFile))
                yield break;

            var configuration = JsonConvert.DeserializeObject<IEnumerable<ApplicationsConfig>>(File.ReadAllText(ConfigFile));

            foreach (var application in configuration)
                yield return CreateApplication(application.Path, application.Hosts ?? new List<string>());
        }

        private RunnableApplication CreateApplication(string application, IEnumerable<string> hosts)
        {
            return new RunnableApplication(Environment, application, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                    $"Applications\\{ToRepeatableFolderName(application)}"), hosts.Select(x => new ApplicationHost(x)).ToList());
        }

        public class ApplicationsConfig
        {
            public string Path { get; set; }
            public IEnumerable<string> Hosts { get; set; }
        }
    }
}