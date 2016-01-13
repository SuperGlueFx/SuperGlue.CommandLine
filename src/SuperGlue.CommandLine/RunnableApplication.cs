using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly IEnumerable<IApplicationHost> _hosts;
        private readonly ICollection<FileListener> _fileListeners = new List<FileListener>();
        private AppDomain _appDomain;
        private RemoteBootstrapper _bootstrapper;

        public RunnableApplication(string environment, string source, string destination, IEnumerable<IApplicationHost> hosts)
        {
            _environment = environment;
            _source = source;
            _destination = destination;
            _hosts = hosts;
        }

        public async Task Start()
        {
            if (_appDomain != null || _bootstrapper != null)
                await Stop();

            StopListeners();

            if (Directory.Exists(_destination))
                new DirectoryInfo(_destination).DeleteDirectoryAndChildren();

            DirectoryCopy(_source, _destination);

            foreach (var host in _hosts)
                await host.Prepare(_destination);

            TransformConfigurationsIn(_destination, ".config", _environment);
            TransformConfigurationsIn(_destination, ".xml", _environment);

            _appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, new AppDomainSetup
            {
                ConfigurationFile = "App.config",
                PrivateBinPath = _destination,
                ApplicationBase = _destination
            });

            _bootstrapper = (RemoteBootstrapper)_appDomain
                .CreateInstanceAndUnwrap(typeof(RemoteBootstrapper).Assembly.FullName, typeof(RemoteBootstrapper).FullName);

            _bootstrapper.Initialize(_destination);
            _bootstrapper.Start(_environment);

            var listener = new FileListener();

            listener.StartListening(_source, "*", x => Recycle().Wait());

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
                await host.TearDown(_destination);

            if (Directory.Exists(_destination))
                new DirectoryInfo(_destination).DeleteDirectoryAndChildren();
        }

        public async Task Recycle()
        {
            await Stop();
            await Start();
        }

        private void StopListeners()
        {
            foreach (var fileListener in _fileListeners)
                fileListener.StopListeners();

            _fileListeners.Clear();
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
                file.CopyTo(temppath, false);
            }

            foreach (var subdir in dirs)
            {
                var temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
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