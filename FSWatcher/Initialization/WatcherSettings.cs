using System;
using System.IO;
using System.Threading;
using FSWatcher.Caching;
using FSWatcher.EventedWatchers;

namespace FSWatcher.Initialization
{
	class SettingsReader
	{
		private static bool _canDetectDirectoryCreate;
		private static bool _canDetectDirectoryDelete;
		private static bool _canDetectFileCreate;
		private static bool _canDetectFileChange;
		private static bool _canDetectFileDelete;

		public static WatcherSettings GetSettings()
		{
            var maxWaitTime = 3000;

			var file2Deleted = false;
			var file3Created = false;
			
			var dir2Deleted = false;
			var dir3Created = false;

			var changeDir = Path.GetTempFileName();
			changeDir = Path.Combine(
				Path.GetDirectoryName(changeDir),
				"changedir_" + Path.GetFileNameWithoutExtension(changeDir));
			Directory.CreateDirectory(changeDir);
			var subdir = Path.Combine(changeDir, "subdir");
			var subdirDelete = Path.Combine(changeDir, "subdirdelete");
			var dir2 = Path.Combine(changeDir, "subdir1");
			var dir3 = Path.Combine(changeDir, "subdir2");
			var file = Path.Combine(subdir, "myfile.txt");
			var file2 = Path.Combine(changeDir, "MovedFile.txt");
			var file3 = file2 + ".again";
			var fileContentChange = Path.Combine(changeDir, "contentToChange.txt");
			Directory.CreateDirectory(dir2);
			Directory.CreateDirectory(subdirDelete);
			File.WriteAllText(fileContentChange, "to be changed");
			File.WriteAllText(file2, "hey");

			var cache = new Cache(changeDir, () => false);
            cache.Initialize();

            Func<bool> fullSupport = () => {
                return
                    _canDetectDirectoryCreate &&
				    _canDetectDirectoryDelete &&
				    dir2Deleted && dir3Created &&
				    _canDetectFileCreate &&
				    _canDetectFileChange &&
				    _canDetectFileDelete &&
				    file2Deleted && file3Created;
            };

			var fsw = new FSW(
				changeDir,
				(s) => {
                    cache.Patch(new Change(ChangeType.DirectoryCreated, s));
					_canDetectDirectoryCreate = true;
					if (s == dir3)
						dir3Created = true;
				},
				(s) => {
                    cache.Patch(new Change(ChangeType.DirectoryDeleted, s));
					_canDetectDirectoryDelete = true;
					if (s == dir2)
						dir2Deleted = true;
				},
				(s) => {
					_canDetectFileCreate = true;
					if (s == file3)
						file3Created = true;
				},
				(s) => _canDetectFileChange = true,
				(s) => {
					_canDetectFileDelete = true;
					if (s == file2)
						file2Deleted = true;
				},
				(s) => {},
                cache);
			fsw.Start();
			
			var fileChanges = new Thread(() => {
                var startTime = DateTime.Now;
				Directory.CreateDirectory(subdir);
				File.WriteAllText(file, "hey");
				using (var writer = File.AppendText(fileContentChange)) {
					writer.Write("moar content");
				}
				File.Move(file2, file3);
				File.Delete(file);
				Directory.Move(dir2, dir3);
				Directory.Delete(subdirDelete);
                while (!fullSupport() && timeSince(startTime) < maxWaitTime)
				    Thread.Sleep(10);
			});
			fileChanges.Start();
			fileChanges.Join();

			fsw.Stop();
			
			Directory.Delete(dir3);
			if (Directory.Exists(subdirDelete))
				Directory.Delete(subdirDelete);
			File.Delete(fileContentChange);
			File.Delete(file3);
			Directory.Delete(subdir);
			Directory.Delete(changeDir);

			return new WatcherSettings(
				_canDetectDirectoryCreate,
				_canDetectDirectoryDelete,
				dir2Deleted && dir3Created,
				_canDetectFileCreate,
				_canDetectFileChange,
				_canDetectFileDelete,
				file2Deleted && file3Created);
		}

        private static double timeSince(DateTime startTime)
        {
            return DateTime.Now.Subtract(startTime).TotalMilliseconds;
        }
	}

	public class WatcherSettings
	{
        public bool ContinuousPolling {
            get {
                var supportsAll =
                    CanDetectEventedDirectoryCreate &&
                    CanDetectEventedDirectoryDelete &&
                    CanDetectEventedDirectoryRename &&
                    CanDetectEventedFileCreate &&
                    CanDetectEventedFileChange &&
                    CanDetectEventedFileDelete &&
                    CanDetectEventedFileRename;
                return !supportsAll;
            }
        }

		public bool CanDetectEventedDirectoryCreate { get; private set; }
		public bool CanDetectEventedDirectoryDelete { get; private set; }
		public bool CanDetectEventedDirectoryRename { get; private set; }
		public bool CanDetectEventedFileCreate { get; private set; }
		public bool CanDetectEventedFileChange { get; private set; }
		public bool CanDetectEventedFileDelete { get; private set; }
		public bool CanDetectEventedFileRename { get; private set; }
        public int PollFrequency { get; private set; }

		public WatcherSettings(
			bool canDetectDirectoryCreate,
			bool canDetectDirectoryDelete,
			bool canDetectDirectoryRename,
			bool canDetectFileCreate,
			bool canDetectFileChange,
			bool canDetectFileDelete,
			bool canDetectFileRename)
		{
			CanDetectEventedDirectoryCreate = canDetectDirectoryCreate;
			CanDetectEventedDirectoryDelete = canDetectDirectoryDelete;
			CanDetectEventedDirectoryRename = canDetectDirectoryRename;
			CanDetectEventedFileCreate = canDetectFileCreate;
			CanDetectEventedFileChange = canDetectFileChange;
			CanDetectEventedFileDelete = canDetectFileDelete;
			CanDetectEventedFileRename = canDetectFileRename;
            PollFrequency = 100;
		}

        public void SetPollFrequencyTo(int milliseconds)
        {
            if (milliseconds > 100)
                PollFrequency = milliseconds;
        }
    }
}
