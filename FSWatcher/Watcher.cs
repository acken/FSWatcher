using System;
using System.Threading;
using FSWatcher.Caching;
using FSWatcher.EventedWatchers;
using FSWatcher.Initialization;

namespace FSWatcher
{
	public class Watcher : IDisposable
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
		private Action<string, Exception> _onError = null;
		private Thread _watcher = null;
        
        private DateTime _nextCatchup = DateTime.MinValue;

		public WatcherSettings Settings { get { return _settings; } }

		public Watcher(
			string dir,
			Action<string> directoryCreated,
			Action<string> directoryDeleted,
			Action<string> fileCreated,
			Action<string> fileChanged,
			Action<string> fileDeleted)
		{
			_dir = dir;
			_cache = new Cache(_dir, () => _exit);	
			_settings = SettingsReader.GetSettings();

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
				_fsw = new FSW(
					_dir,
					(dir) => {
						if (_cache.Patch(new Change(ChangeType.DirectoryCreated, dir)))
						    _directoryCreated(dir);
                        setNextCatchup();
					},
					(dir) => {
						if (_cache.Patch(new Change(ChangeType.DirectoryDeleted, dir)))
						    _directoryDeleted(dir);
                        setNextCatchup();
					},
					(file) => {
						if (_cache.Patch(new Change(ChangeType.FileCreated, file)))
						    _fileCreated(file);
                        setNextCatchup();
					},
					(file) => {
						if (_cache.Patch(new Change(ChangeType.FileChanged, file)))
						    _fileChanged(file);
                        setNextCatchup();
					},
					(file) => {
						if (_cache.Patch(new Change(ChangeType.FileDeleted, file)))
						    _fileDeleted(file);
                        setNextCatchup();
					},
					(item) => {
                        setNextCatchup();
					},
					_cache);
				_fsw.Start();

				while (!_exit) {
					if (_fsw.NeedsRestart) {
						_fsw.Start();
						setNextCatchup();
					}
						
                    if (weNeedToCatchUp())
					    poll();
                    if (_settings.ContinuousPolling && !waitingToCatchUp())
                        setNextCatchup();
					Thread.Sleep(_settings.PollFrequency + 10);
				}
				_fsw.Stop();
			});
			_watcher.Priority = ThreadPriority.BelowNormal;
			_watcher.Start();
		}

        public void ForceRefresh()
        {
            poll();
        }

		public void StopWatching()
		{
			_exit = true;
			if (_watcher == null)
				return;
			while (_watcher.IsAlive)
				Thread.Sleep(10);
		}

		public void ErrorNotifier(Action<string, Exception> notifier)
		{
			_onError = notifier;
			_cache.ErrorNotifier(notifier);
		}

		public void Dispose()
		{
			StopWatching();
		}

		private void initialize()
		{
            var startTime = DateTime.Now;
			_cache.Initialize();
            _settings.SetPollFrequencyTo(timeSince(startTime) * 4);
		}

        private int timeSince(DateTime time)
        {
            return Convert.ToInt32(DateTime.Now.Subtract(time).TotalMilliseconds);
        }

		private void poll()
		{
			var hasChanges = _cache.RefreshFromDisk(
			    _directoryCreated,
			    _directoryDeleted,
			    _fileCreated,
			    _fileChanged,
			    _fileDeleted);
            clearCatchup();
            if (hasChanges)
                setNextCatchup();
		}

        private bool weNeedToCatchUp()
        {
            return _nextCatchup != DateTime.MinValue && DateTime.Now > _nextCatchup;
        }

		private bool waitingToCatchUp()
		{
			return _nextCatchup != DateTime.MinValue;
		}

        private void clearCatchup()
        {
            _nextCatchup = DateTime.MinValue;
        }

        private void setNextCatchup()
        {
            _nextCatchup = DateTime.Now.AddMilliseconds(_settings.PollFrequency);
        }
	}
}
