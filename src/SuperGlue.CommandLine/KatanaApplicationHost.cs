using System.IO;
using System.Threading.Tasks;

namespace SuperGlue
{
    public class KatanaApplicationHost : IApplicationHost
    {
        private readonly string _hostDirectory;

        public KatanaApplicationHost(string hostDirectory)
        {
            _hostDirectory = hostDirectory;
        }

        public Task Prepare(string applicationPath)
        {
            var filesToCopy = Directory.GetFiles(_hostDirectory, "*Owin*.dll");

            foreach (var file in filesToCopy)
                File.Copy(file, Path.Combine(applicationPath, Path.GetFileName(file) ?? ""), true);

            File.Copy(Path.Combine(_hostDirectory, "SuperGlue.Hosting.Katana.dll"), Path.Combine(applicationPath, "SuperGlue.Hosting.Katana.dll"), true);

            return Task.CompletedTask;
        }

        public Task TearDown(string applicationPath)
        {
            return Task.CompletedTask;
        }
    }
}