using System;
using System.IO;

namespace SuperGlue
{
    public class FileListener 
    {
        private FileSystemWatcher _fileSystemWatcher;

        public void StartListening(string directory, string filter, Action<string> updated)
        {
            if (_fileSystemWatcher != null)
                _fileSystemWatcher.Dispose();

            _fileSystemWatcher = new FileSystemWatcher(directory, filter)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            _fileSystemWatcher.Created += (sender, eventArgs) =>
            {
                updated(eventArgs.FullPath);
            };

            _fileSystemWatcher.Changed += (sender, eventArgs) =>
            {
                updated(eventArgs.FullPath);
            };

            _fileSystemWatcher.Deleted += (sender, eventArgs) =>
            {
                updated(eventArgs.FullPath);
            };

            _fileSystemWatcher.Renamed += (sender, eventArgs) =>
            {
                updated(eventArgs.FullPath);
            };
        }

        public void StopListeners()
        {
            if (_fileSystemWatcher == null) 
                return;

            _fileSystemWatcher.Dispose();
            _fileSystemWatcher = null;
        }
    }
}