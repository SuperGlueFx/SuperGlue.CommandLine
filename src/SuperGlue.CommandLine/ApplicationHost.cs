using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SuperGlue
{
    public class ApplicationHost
    {
        private readonly string _name;

        public ApplicationHost(string name)
        {
            _name = name;
        }

        public Task Prepare(string applicationPath)
        {
            var hostDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", $"Hosts\\{_name}");

            if(!Directory.Exists(hostDirectory))
                return Task.CompletedTask;

            var filesToCopy = Directory.GetFiles(hostDirectory);

            foreach (var file in filesToCopy)
                File.Copy(file, Path.Combine(applicationPath, Path.GetFileName(file) ?? ""), true);

            return Task.CompletedTask;
        }

        public Task TearDown(string applicationPath)
        {
            return Task.CompletedTask;
        }
    }
}