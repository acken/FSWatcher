using System;
using System.Threading;
using FSWatcher.Caching;
using FSWatcher.EventedWatchers;
using FSWatcher.Initialization;

namespace FSWatcher
{
	public class Watcher
	{
		private string _dir;
		private bool _exit = false;
		private WatcherSettings _settings;
		private Cache _cache;
		private FSW _fsw;
		private Action<string> _fileCreated;
		private Action<string> _fileChanged;
		private Action<string> _fileDeleted;
		private Action<string> _directoryCreated;
		private Action<string> _directoryDeleted;
		private Thread _watcher = null;

		public Watcher(
			string dir,
			Action<string> directoryCreated,
			Action<string> directoryDeleted,
			Action<string> fileCreated,
			Action<string> fileChanged,
			Action<string> fileDeleted)
		{
			_dir = dir;
			_cache = new Cache(_dir);	
			_settings = WatcherSettings.GetSettings(_cache);

			Console.WriteLine("Detects create directory: " + _settings.CanDetectDirectoryCreate.ToString());
			Console.WriteLine("Detects delete directory: " + _settings.CanDetectDirectoryDelete.ToString());
			Console.WriteLine("Detects rename directory: " + _settings.CanDetectDirectoryRename.ToString());
			Console.WriteLine("Detects create file: " + _settings.CanDetectFileCreate.ToString());
			Console.WriteLine("Detects change file: " + _settings.CanDetectFileChange.ToString());
			Console.WriteLine("Detects delete file: " + _settings.CanDetectFileDelete.ToString());
			Console.WriteLine("Detects rename file: " + _settings.CanDetectFileRename.ToString());

			_directoryCreated = directoryCreated;
			_directoryDeleted = directoryDeleted;
			_fileCreated = fileCreated;
			_fileChanged = fileChanged;
			_fileDeleted = fileDeleted;
		}

		// when reciving events do _cache.Patch(file);
		// patch adds to a queue
		// no locking required
		// in that way the cache can update right before doing a manual refresh
		// or automatically if polling not enabled
		public void Watch()
		{
			_watcher = new Thread(() => {
				initialize();
				_fsw = new FSW(
					_dir,
					(dir) => {
						_cache.Patch(new Change(ChangeType.DirectoryCreated, dir));
						_directoryCreated(dir);
					},
					(dir) => {
						_cache.Patch(new Change(ChangeType.DirectoryDeleted, dir));
						_directoryDeleted(dir);
					},
					(file) => {
						_cache.Patch(new Change(ChangeType.FileCreated, file));
						_fileCreated(file);
					},
					(file) => {
						_cache.Patch(new Change(ChangeType.FileChanged, file));
						_fileChanged(file);
					},
					(file) => {
						_cache.Patch(new Change(ChangeType.FileDeleted, file));
						_fileDeleted(file);
					},
					(item) => {
						poll();
					},
					_cache);

				while (!_exit) {
					poll();
					Thread.Sleep(500);
				}
				_fsw.Stop();
			});
			_watcher.Start();
		}

		public void StopWatching()
		{
			_exit = true;
			if (_watcher == null)
				return;
			while (_watcher.IsAlive)
				Thread.Sleep(10);
		}

		private void initialize()
		{
			_cache.Initialize();
		}

		private void poll()
		{
			_cache.RefreshFromDisk(
			_directoryCreated,
			_directoryDeleted,
			_fileCreated,
			_fileChanged,
			_fileDeleted);
		}
	}
}
