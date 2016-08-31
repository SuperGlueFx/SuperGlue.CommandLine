using System;
using System.IO;
using System.Threading.Tasks;

namespace SuperGlue
{
    public class FileListener 
    {
        private FileSystemWatcher _fileSystemWatcher;

        public void StartListening(string directory, string filter, Func<string, Task> updated)
        {
            _fileSystemWatcher?.Dispose();

            _fileSystemWatcher = new FileSystemWatcher(directory, filter)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            _fileSystemWatcher.Created += async (sender, eventArgs) =>
            {
                await updated(eventArgs.FullPath).ConfigureAwait(false);
            };

            _fileSystemWatcher.Changed += async (sender, eventArgs) =>
            {
                await updated(eventArgs.FullPath).ConfigureAwait(false);
            };

            _fileSystemWatcher.Deleted += async (sender, eventArgs) =>
            {
                await updated(eventArgs.FullPath).ConfigureAwait(false);
            };

            _fileSystemWatcher.Renamed += async (sender, eventArgs) =>
            {
                await updated(eventArgs.FullPath).ConfigureAwait(false);
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