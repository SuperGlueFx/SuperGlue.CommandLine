using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Web.XmlTransform;
using SuperGlue.Configuration;

namespace SuperGlue
{
    public class RunnableApplication
    {
        private readonly string _environment;
        private readonly string _source;
        private readonly string _destination;
        private readonly IEnumerable<ApplicationHost> _hosts;
        private readonly IEnumerable<string> _ignoredPaths;
        private readonly IDictionary<string, string[]> _hostArguments;
        private readonly ICollection<FileListener> _fileListeners = new List<FileListener>();
        private AppDomain _appDomain;
        private RemoteBootstrapper _bootstrapper;

        public RunnableApplication(string environment, string source, string destination, string applicationName, IEnumerable<ApplicationHost> hosts, IDictionary<string, string[]> hostArguments, params string[] ignoredPaths)
        {
            _environment = environment;
            _source = source;
            _destination = destination;
            _hosts = hosts;
            _hostArguments = hostArguments;
            _ignoredPaths = ignoredPaths;
            ApplicationName = applicationName;
        }

        public string ApplicationName { get; }

        public async Task Start()
        {
            if (_appDomain != null || _bootstrapper != null)
                await Stop().ConfigureAwait(false);

            StopListeners();

            if (Directory.Exists(_destination))
                new DirectoryInfo(_destination).DeleteDirectoryAndChildren();

            DirectoryCopy(_source, _destination);

            foreach (var host in _hosts)
                await host.Prepare(_destination).ConfigureAwait(false);

            TransformConfigurationsIn(_destination, ".config", _environment);
            TransformConfigurationsIn(_destination, ".xml", _environment);

            _appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ConfigurationFile = $"{ApplicationName}.dll.config",
                PrivateBinPath = _destination,
                ApplicationBase = _destination
            });

            _bootstrapper = (RemoteBootstrapper)_appDomain
                .CreateInstanceAndUnwrap(typeof(RemoteBootstrapper).Assembly.FullName, typeof(RemoteBootstrapper).FullName);

            _bootstrapper.Initialize(_destination);
            _bootstrapper.Start(_environment, _hostArguments);

            var listener = new FileListener();

            var extensionsNeedingReload = new List<string>
            {
                ".dll",
                ".exe",
                ".config",
                ".xml"
            };

            listener.StartListening(_source, "*", async x =>
            {
                var extension = Path.GetExtension(x);
                if (extensionsNeedingReload.Contains(extension))
                {
                    await Recycle().ConfigureAwait(false);

                    return;
                }

                var relativePath = x.Replace(_source, "");

                if (relativePath.StartsWith("\\"))
                    relativePath = relativePath.Substring(1);

                var newPath = Path.Combine(_destination, relativePath);

                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        if (ShouldCopy(Path.GetDirectoryName(x)))
                            File.Copy(x, newPath, true);

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed copying file: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                }
            });

            _fileListeners.Add(listener);
        }

        public async Task Stop()
        {
            StopListeners();

            if (_bootstrapper != null)
            {
                _bootstrapper.Stop();
                _bootstrapper = null;
            }

            if (_appDomain != null)
            {
                AppDomain.Unload(_appDomain);
                _appDomain = null;
            }

            foreach (var host in _hosts)
                await host.TearDown(_destination).ConfigureAwait(false);

            if (Directory.Exists(_destination))
                new DirectoryInfo(_destination).DeleteDirectoryAndChildren();
        }

        public async Task Recycle()
        {
            await Stop().ConfigureAwait(false);
            await Start().ConfigureAwait(false);
        }

        private void StopListeners()
        {
            foreach (var fileListener in _fileListeners)
                fileListener.StopListeners();

            _fileListeners.Clear();
        }

        private void DirectoryCopy(string sourceDirName, string destDirName)
        {
            if (!ShouldCopy(sourceDirName))
                return;

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

        private bool ShouldCopy(string directory)
        {
            return !_ignoredPaths.Any(x => Regex.IsMatch(directory, x));
        }

        private static void TransformConfigurationsIn(string directory, string configExtension, string transformation)
        {
            var configFiles = Directory.GetFiles(directory, $"*{configExtension}").ToList();

            var transformationFiles = new List<string>();

            foreach (var configFile in configFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(configFile);

                if (string.IsNullOrEmpty(fileName))
                    continue;

                transformationFiles.AddRange(configFiles.Where(x => x.StartsWith(fileName) && Path.GetFileNameWithoutExtension(x) != fileName));
            }

            var filesToTransform = configFiles.Except(transformationFiles);

            foreach (var file in filesToTransform)
            {
                var transformationFile = $"{file}.{transformation}{configExtension}";

                if (File.Exists(transformationFile))
                    TransformConfig($"{file}{configExtension}", transformationFile);
            }

            foreach (var child in Directory.GetDirectories(directory))
                TransformConfigurationsIn(child, configExtension, transformation);
        }

        private static void TransformConfig(string configFileName, string transformFileName)
        {
            var document = new XmlTransformableDocument
            {
                PreserveWhitespace = true
            };

            document.Load(configFileName);

            var transformation = new XmlTransformation(transformFileName);

            if (!transformation.Apply(document))
                throw new Exception("Transformation Failed");

            document.Save(configFileName);
        }
    }
}