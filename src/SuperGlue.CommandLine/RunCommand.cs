using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuperGlue
{
    public class RunCommand : ICommand
    {
        public RunCommand()
        {
            Hosts = new List<string>();
        }

        public string Application { get; set; }
        public string Environment { get; set; }
        public ICollection<string> Hosts { get; set; }

        public async Task Execute()
        {
            var application = new RunnableApplication(Environment, Application, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", $"Applications\\{ToRepeatableFolerName(Application)}"),
                Hosts.Select(x => new ApplicationHost(x)).ToList());

            await application.Start().ConfigureAwait(false);

            var key = Console.ReadKey();

            while (key.Key != ConsoleKey.Q)
            {
                if (key.Key != ConsoleKey.R)
                    continue;

                Console.WriteLine();

                await application.Recycle().ConfigureAwait(false);

                key = Console.ReadKey();
            }

            await application.Stop().ConfigureAwait(false);
        }

        private static string ToRepeatableFolerName(string input)
        {
            var hasher = new SHA1Managed();
            var bytes = Encoding.ASCII.GetBytes(input);
            var hash = hasher.ComputeHash(bytes);

            return Regex.Replace(Convert.ToBase64String(hash), @"[^a-zÂ‰ˆ¯ÊA-Z≈ƒ÷ÿ∆0-9!@#\-]+", "");
        }
    }
}