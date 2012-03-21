using System;
using System.Linq;
using System.Collections.Generic;
using FSWatcher.FS;

namespace FSWatcher.Caching
{
	class Cache
	{
		private string _dir;
		private Dictionary<int, string> _directories = new Dictionary<int, string>();
		private Dictionary<int, File> _files = new Dictionary<int, File>();

		public Cache(string dir)
		{
			_dir = dir;
		}
		
		public void Initialize()
		{
			getSnapshot(_dir, ref _directories, ref _files);
		}

		public void GenerateEvents(
			Action<string> directoryCreated,
			Action<string> directoryDeleted,
			Action<string> fileCreated,
			Action<string> fileChanged,
			Action<string> fileDeleted)
		{
			var dirs = new Dictionary<int, string>();
			var files = new Dictionary<int, File>();
			getSnapshot(_dir, ref dirs, ref files);
			
			handleDeleted(_directories, dirs, directoryDeleted);
			handleCreated(_directories, dirs, directoryCreated);
			handleDeleted(_files, files, fileDeleted);
			handleCreated(_files, files, fileCreated);
			handleChanged(_files, files, fileChanged);
		}

		private void getSnapshot(
			string directory,
			ref Dictionary<int, string> dirs,
			ref Dictionary<int, File> files)
		{
			foreach (var dir in System.IO.Directory.GetDirectories(directory)) {
				dirs.Add(dir.GetHashCode(), dir);
				getSnapshot(dir, ref dirs, ref files);
			}

			foreach (var filepath in System.IO.Directory.GetFiles(directory)) {
				var file = new File(filepath, _dir.GetHashCode());
				files.Add(file.GetHashCode(), file);
			}
		}

		private void handleCreated<T>(
			Dictionary<int, T> original,
			Dictionary<int, T> items,
			Action<string> action)
		{
			getCreated(original, items)
				.ForEach(x => {
					add(x, original);
					notify(x.ToString(), action);
				});
		}
		
		private void handleChanged(
			Dictionary<int, File> original,
			Dictionary<int, File> items,
			Action<string> action)
		{
			getChanged(original, items)
				.ForEach(x => {
					File file;
					if (original.TryGetValue(x.GetHashCode(), out file))
						file.SetHash(x.Hash);
					notify(x.ToString(), action);
				});
		}

		private void handleDeleted<T>(
			Dictionary<int, T> original,
			Dictionary<int, T> items,
			Action<string> action)
		{
			getDeleted(original, items)
				.ForEach(x => {
					remove(x.GetHashCode(), original);
					notify(x.ToString(), action);
				});
		}

		private void add<T>(T item, Dictionary<int, T> list)
		{
			list.Add(item.GetHashCode(), item);
		}

		private void remove<T>(int item, Dictionary<int, T> list)
		{
			list.Remove(item);
		}
		
		private void notify(string item, Action<string> action)
		{
			if (action != null)
				action(item.ToString());
		}

		private List<T> getCreated<T>(
			Dictionary<int, T> original,
			Dictionary<int, T> items)
		{
			var added = new List<T>();
			foreach (var item in items)
				if (!original.ContainsKey(item.Key))
					added.Add(item.Value);
			return added;
		}

		private List<File> getChanged(
			Dictionary<int, File> original,
			Dictionary<int, File> items)
		{
			var changed = new List<File>();
			foreach (var item in items)
			{
				File val;
				if (original.TryGetValue(item.Key, out val))
				{
					if (val.Hash != item.Value.Hash)
						changed.Add(item.Value);
				}
			}
			return changed;
		}
		
		private List<T> getDeleted<T>(
			Dictionary<int, T> original,
			Dictionary<int, T> items)
		{
			var deleted = new List<T>();
			foreach (var item in original)
				if (!items.ContainsKey(item.Key))
					deleted.Add(item.Value);
			return deleted;
		}
	}
}
