using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SuperGlue
{
    public class InstallCommand : ICommand
    {
        public InstallCommand()
        {
            Hosts = new List<string>();
        }

        public string Installer { get; set; }
        public string Application { get; set; }
        public string Environment { get; set; }
        public ICollection<string> Hosts { get; set; }

        public Task Execute()
        {
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            var installerDirectory = Path.Combine(location, $"Installers\\{Installer}");

            var installerFile = Path.GetFileName(Directory.GetFiles(installerDirectory, "*.exe").FirstOrDefault() ?? "");

            DirectoryCopy(installerDirectory, Application);

            foreach (var host in Hosts)
                DirectoryCopy(Path.Combine(location, $"Hosts\\{host}"), Application);

            if (string.IsNullOrEmpty(installerFile))
                return Task.CompletedTask;

            var applicationName = GetApplicationName(Application);

            File.Copy(Path.Combine(Application, $"{applicationName}.dll.config"), Path.Combine(Application, $"{installerFile}.config"));

            var startInfo = new ProcessStartInfo(Path.Combine(Application, installerFile), $"install -appname:\"{applicationName}\" -environment:{Environment}")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Application,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (x, y) => Console.WriteLine(y.Data);

            if (process.Start())
                process.WaitForExit();

            return Task.CompletedTask;
        }

        private static string GetApplicationName(string path)
        {
            var invalidPaths = new List<string>
            {
                "bin",
                "obj",
                "debug",
                "release",
                "src"
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

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();

            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            foreach (var subdir in dirs)
            {
                var temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }
    }
}