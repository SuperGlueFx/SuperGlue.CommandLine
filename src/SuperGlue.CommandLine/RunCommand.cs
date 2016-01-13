using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SuperGlue
{
    public class RunCommand : ICommand
    {
        private static readonly IDictionary<string, Func<RunCommand, IApplicationHost>> AppHostFactories = new Dictionary<string, Func<RunCommand, IApplicationHost>>
        {
            {"katana", BuildKatanaApplicationHost}
        };

        public RunCommand()
        {
            Hosts = new List<string>();
        }

        public string Application { get; set; }
        public string Environment { get; set; }
        public ICollection<string> Hosts { get; set; }

        public async Task Execute()
        {
            var application = new RunnableApplication(Environment, Application, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", $"Applications\\{HashUsingSha1(Application)}"),
                Hosts.Where(x => AppHostFactories.ContainsKey(x)).Select(x => AppHostFactories[x](this)).ToList());

            await application.Start();

            var key = Console.ReadKey();

            while (key.Key != ConsoleKey.Q)
            {
                if (key.Key != ConsoleKey.R)
                    continue;

                Console.WriteLine();

                await application.Recycle();

                key = Console.ReadKey();
            }

            await application.Stop();
        }

        private static string HashUsingSha1(string input)
        {
            var hasher = new SHA1Managed();
            var passwordBytes = Encoding.ASCII.GetBytes(input);
            var passwordHash = hasher.ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordHash);
        }

        private static KatanaApplicationHost BuildKatanaApplicationHost(RunCommand command)
        {
            return new KatanaApplicationHost(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "");
        }
    }
}