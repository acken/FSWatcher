using System;
using System.Linq;
using System.Collections.Generic;
using FSWatcher.FS;

namespace FSWatcher.Caching
{
	enum ChangeType
	{
		DirectoryCreated,
		DirectoryDeleted,
		FileCreated,
		FileChanged,
		FileDeleted
	}

	class Change
	{
		public ChangeType Type { get; private set; }
		public string Item { get; private set; }

		public Change(ChangeType type, string item)
		{
			Type = type;
			Item = item;
		}
	}
	class Cache
	{
		private Func<bool> _abortCheck;
		private string _dir;
		private Dictionary<int, string> _directories = new Dictionary<int, string>();
		private Dictionary<int, File> _files = new Dictionary<int, File>();

		public Cache(string dir, Func<bool> abortCheck)
		{
			_dir = dir;
			_abortCheck = abortCheck;
		}
		
		public void Initialize()
		{
			getSnapshot(_dir, ref _directories, ref _files);
		}

		public bool IsDirectory(string dir) {
			return _directories.ContainsKey(dir.GetHashCode());
		}

		public bool RefreshFromDisk(
			Action<string> directoryCreated,
			Action<string> directoryDeleted,
			Action<string> fileCreated,
			Action<string> fileChanged,
			Action<string> fileDeleted)
		{
			var dirs = new Dictionary<int, string>();
			var files = new Dictionary<int, File>();
			getSnapshot(_dir, ref dirs, ref files);
			
            var hasChanges = false;
			if (handleDeleted(_directories, dirs, directoryDeleted))
                hasChanges = true;
			if (handleCreated(_directories, dirs, directoryCreated))
                hasChanges = true;
			if (handleDeleted(_files, files, fileDeleted))
                hasChanges = true;
			if (handleCreated(_files, files, fileCreated))
                hasChanges = true;
			if (handleChanged(_files, files, fileChanged))
                hasChanges = true;
            return hasChanges;
		}
		
		public bool Patch(Change item) {
            return applyPatch(item);
		}

        private bool applyPatch(Change item)
		{
			if (item == null)
				return false;
			if (item.Type == ChangeType.DirectoryCreated) {
				return add(item.Item, _directories);
            }
			if (item.Type == ChangeType.DirectoryDeleted) {
				return remove(item.Item.GetHashCode(), _directories);
            }
			if (item.Type == ChangeType.FileCreated) {
				return add(getFile(item.Item), _files);
            }
			if (item.Type == ChangeType.FileChanged) {
				return update(getFile(item.Item), _files);
            }
			if (item.Type == ChangeType.FileDeleted) {
				return remove(getFile(item.Item).GetHashCode(), _files);
            }
            return false;
		}

		private File getFile(string file)
		{
			return new File(file, System.IO.Path.GetDirectoryName(file).GetHashCode());
		}

		private void getSnapshot(
			string directory,
			ref Dictionary<int, string> dirs,
			ref Dictionary<int, File> files)
		{
			if (_abortCheck())
				return;
			foreach (var dir in System.IO.Directory.GetDirectories(directory)) {
				dirs.Add(dir.GetHashCode(), dir);
				getSnapshot(dir, ref dirs, ref files);
			}

			foreach (var filepath in System.IO.Directory.GetFiles(directory)) {
				var file = getFile(filepath);
				files.Add(file.GetHashCode(), file);
				if (_abortCheck())
					return;
			}
		}

		private bool handleCreated<T>(
			Dictionary<int, T> original,
			Dictionary<int, T> items,
			Action<string> action)
		{
            var hasChanges = false;
			getCreated(original, items)
				.ForEach(x => {
					if (add(x, original)) {
						notify(x.ToString(), action);
						hasChanges = true;
					}
				});
            return hasChanges;
		}

        private bool handleChanged(
			Dictionary<int, File> original,
			Dictionary<int, File> items,
			Action<string> action)
		{
            var hasChanges = false;
			getChanged(original, items)
				.ForEach(x => {
					if (update(x, original)) {
						notify(x.ToString(), action);
						hasChanges = true;
					}
				});
            return hasChanges;
		}

        private bool handleDeleted<T>(
			Dictionary<int, T> original,
			Dictionary<int, T> items,
			Action<string> action)
		{
            var hasChanges = false;
			getDeleted(original, items)
				.ForEach(x => {
					if (remove(x.GetHashCode(), original)) {
						notify(x.ToString(), action);
						hasChanges = true;
					}
				});
            return hasChanges;
		}

		private bool add<T>(T item, Dictionary<int, T> list)
		{
            lock (list) {
                var key = item.GetHashCode();
                if (!list.ContainsKey(key)) {
			        list.Add(key, item);
                    return true;
                }
            }
            return false;
		}

		private bool remove<T>(int item, Dictionary<int, T> list)
		{
            lock (list) {
                if (list.ContainsKey(item))
                {
                    list.Remove(item);
                    return true;
                }
            }
            return false;
		}

		private bool update(File file, Dictionary<int, File> list)
		{
            lock (list) {
			    File originalFile;
			    if (list.TryGetValue(file.GetHashCode(), out originalFile)) {
					if (!originalFile.Hash.Equals(file.Hash)) {
						originalFile.SetHash(file.Hash);
						return true;
					}
                }
            }
            return false;
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
