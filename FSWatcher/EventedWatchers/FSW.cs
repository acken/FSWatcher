using System;
using System.IO;
using System.Collections.Generic;
using FSWatcher.Caching;

namespace FSWatcher.EventedWatchers
{
	class FSW
	{
		private string _watchPath;
		private Cache _cache;
		private FileSystemWatcher _watcher;
		private Action<string> _fileCreated;
		private Action<string> _fileChanged;
		private Action<string> _fileDeleted;
		private Action<string> _directoryCreated;
		private Action<string> _directoryDeleted;
		private Action<string> _onError;
		
		public FSW(
			string watchPath,
			Action<string> directoryCreated,
			Action<string> directoryDeleted,
			Action<string> fileCreated,
			Action<string> fileChanged,
			Action<string> fileDeleted,
			Action<string> onError,
			Cache cache)
		{
			_watchPath = watchPath;
			_directoryCreated = directoryCreated;
			_directoryDeleted = directoryDeleted;
			_fileCreated = fileCreated;
			_fileChanged = fileChanged;
			_fileDeleted = fileDeleted;
			_onError = onError;
			_cache = cache;
			startListener();
		}

		public void Stop()
		{
			if (_watcher != null)
			{
				_watcher.Changed -= WatcherChangeHandler;
	            _watcher.Created -= WatcherChangeHandler;
	            _watcher.Deleted -= WatcherChangeHandler;
	            _watcher.Renamed -= WatcherRenamedHandler;
	            _watcher.Error -= WatcherErrorHandler;
				_watcher.Dispose();
			}
		}
		
		private void startListener()
		{
			Stop();
			
			_watcher = new FileSystemWatcher
                           {
                               NotifyFilter = 
                                    NotifyFilters.CreationTime |
                                    NotifyFilters.LastWrite |
                                    NotifyFilters.DirectoryName |
                                    NotifyFilters.FileName |
							        NotifyFilters.Size |
                                    NotifyFilters.Attributes,
                               IncludeSubdirectories = true
                           };
			_watcher.Changed += WatcherChangeHandler;
            _watcher.Created += WatcherChangeHandler;
            _watcher.Deleted += WatcherChangeHandler;
            _watcher.Renamed += WatcherRenamedHandler;
            _watcher.Error += WatcherErrorHandler;
			_watcher.Path = _watchPath;
			_watcher.EnableRaisingEvents = true;
		}
		
		private void WatcherChangeHandler(object sender, FileSystemEventArgs e)
        {
			if (e.ChangeType == WatcherChangeTypes.Created) {
				if (Directory.Exists(e.FullPath)) {
					_directoryCreated(e.FullPath);
					if (Environment.OSVersion.Platform == PlatformID.Unix ||
						Environment.OSVersion.Platform == PlatformID.MacOSX) {
						startListener();
					}
				} else
					_fileCreated(e.FullPath);
				return;
			}

			if (e.ChangeType == WatcherChangeTypes.Changed) {
				_fileChanged(e.FullPath);
				return;
			}

			if (e.ChangeType == WatcherChangeTypes.Deleted) {
				if (_cache.IsDirectory(e.FullPath))
					_directoryDeleted(e.FullPath);
				else
					_fileDeleted(e.FullPath);
			}
        }

		private void WatcherRenamedHandler(object sender, RenamedEventArgs e)
		{
            if (_cache.IsDirectory(e.OldFullPath))
            {
                _directoryDeleted(e.OldFullPath);
                _directoryCreated(e.FullPath);
			} else {
                _fileDeleted(e.OldFullPath);
                _fileCreated(e.FullPath);
			}
		}
		
		private void WatcherErrorHandler(object sender, ErrorEventArgs e)
        {
			_onError(e.ToString());
        }
	}
}
