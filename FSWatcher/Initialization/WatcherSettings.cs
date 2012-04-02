using System;
using System.IO;
using System.Threading;
using FSWatcher.Caching;
using FSWatcher.EventedWatchers;

namespace FSWatcher.Initialization
{
	class WatcherSettings
	{
		private static bool _canDetectDirectoryCreate;
		private static bool _canDetectDirectoryDelete;
		private static bool _canDetectDirectoryRename;
		private static bool _canDetectFileCreate;
		private static bool _canDetectFileChange;
		private static bool _canDetectFileDelete;
		private static bool _canDetectFileRename;
		
		public bool CanDetectDirectoryCreate { get; private set; }
		public bool CanDetectDirectoryDelete { get; private set; }
		public bool CanDetectDirectoryRename { get; private set; }
		public bool CanDetectFileCreate { get; private set; }
		public bool CanDetectFileChange { get; private set; }
		public bool CanDetectFileDelete { get; private set; }
		public bool CanDetectFileRename { get; private set; }

		public WatcherSettings(
			bool canDetectDirectoryCreate,
			bool canDetectDirectoryDelete,
			bool canDetectDirectoryRename,
			bool canDetectFileCreate,
			bool canDetectFileChange,
			bool canDetectFileDelete,
			bool canDetectFileRename)
		{
			CanDetectDirectoryCreate = canDetectDirectoryCreate;
			CanDetectDirectoryDelete = canDetectDirectoryDelete;
			CanDetectDirectoryRename = canDetectDirectoryRename;
			CanDetectFileCreate = canDetectFileCreate;
			CanDetectFileChange = canDetectFileChange;
			CanDetectFileDelete = canDetectFileDelete;
			CanDetectFileRename = canDetectFileRename;
		}

		public static WatcherSettings GetSettings(Cache cache)
		{
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
			var dir2 = Path.Combine(changeDir, "subdir1");
			var dir3 = Path.Combine(changeDir, "subdir2");
			var file = Path.Combine(subdir, "myfile.txt");
			var file2 = Path.Combine(changeDir, "MovedFile.txt");
			var file3 = file2 + ".again";
			var fsw = new FSW(
				changeDir,
				(s) => {
					_canDetectDirectoryCreate = true;
					if (s == dir3)
						dir3Created = true;
				},
				(s) => {
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
			Directory.CreateDirectory(subdir);

			
			File.WriteAllText(file, "hey");

			using (var writer = File.AppendText(file)) {
				writer.Write("moar content");
			}
			Thread.Sleep(500);
			
			File.Move(file, file2);
			Thread.Sleep(500);
			
			File.Move(file2, file3);
			Thread.Sleep(500);

			File.Delete(file);

			Directory.Move(subdir, dir2);
			Thread.Sleep(500);

			Directory.Move(dir2, dir3);
			Thread.Sleep(500);

			Directory.Delete(dir3);
			Thread.Sleep(500);

			fsw.Stop();
			
			File.Delete(file2);
			File.Delete(file3);
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
	}
}
