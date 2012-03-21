using System;
using System.Threading;
using FSWatcher.Caching;

namespace FSWatcher
{
	public class Watcher
	{
		private string _dir;
		private bool _exit = false;
		private Cache _cache;
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
			_directoryCreated = directoryCreated;
			_directoryDeleted = directoryDeleted;
			_fileCreated = fileCreated;
			_fileChanged = fileChanged;
			_fileDeleted = fileDeleted;
		}

		public void Watch()
		{
			_watcher = new Thread(() => {
				initialize();
				while (!_exit) {
					poll();
					Thread.Sleep(500);
				}
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
			_cache.GenerateEvents(
			_directoryCreated,
			_directoryDeleted,
			_fileCreated,
			_fileChanged,
			_fileDeleted);
		}
	}
}
