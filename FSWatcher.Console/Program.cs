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
			watcher.Watch();
			System.Console.ReadLine();
			watcher.StopWatching();
		}
	}
}
