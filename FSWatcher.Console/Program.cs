using System;

namespace FSWatcher.Console
{
	class Program
	{
		public static void Main(string[] args)
		{
			var watcher = 
				new Watcher(
					Environment.CurrentDirectory,
					(s) => System.Console.WriteLine("Dir created " + s),
					(s) => System.Console.WriteLine("Dir deleted " + s),
					(s) => System.Console.WriteLine("File created " + s),
					(s) => System.Console.WriteLine("File changed " + s),
					(s) => System.Console.WriteLine("File deleted " + s));
			watcher.ErrorNotifier((path, ex) => { System.Console.WriteLine("{0}\n{1}", path, ex); });

			// Print strategy
			System.Console.WriteLine(
				"Will poll continuously: {0}",
				watcher.Settings.ContinuousPolling);
			System.Console.WriteLine(
				"Poll frequency: {0} milliseconds",
				watcher.Settings.PollFrequency);

			System.Console.WriteLine(
				"Evented directory create: {0}",
				watcher.Settings.CanDetectEventedDirectoryCreate);
			System.Console.WriteLine(
				"Evented directory delete: {0}",
				watcher.Settings.CanDetectEventedDirectoryDelete);
			System.Console.WriteLine(
				"Evented directory rename: {0}",
				watcher.Settings.CanDetectEventedDirectoryRename);
			System.Console.WriteLine(
				"Evented file create: {0}",
				watcher.Settings.CanDetectEventedFileCreate);
			System.Console.WriteLine(
				"Evented file change: {0}",
				watcher.Settings.CanDetectEventedFileChange);
			System.Console.WriteLine(
				"Evented file delete: {0}",
				watcher.Settings.CanDetectEventedFileDelete);
			System.Console.WriteLine(
				"Evented file rename: {0}",
				watcher.Settings.CanDetectEventedFileRename);

			watcher.Watch();
			var command = System.Console.ReadLine();
            if (command == "refresh")
                watcher.ForceRefresh();
			watcher.StopWatching();
		}
	}
}
