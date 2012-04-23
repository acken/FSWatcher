namespace FSWatcher.FS
{
	class File
	{
		public string Path { get; private set; }
		public long Hash { get; private set; }		
		public int Directory { get; private set; }

		public File(string file, int dir) {
			Path = file;
			Directory = dir;
		    Hash = getContentHash();
		}

		public void SetHash(long newHash)
		{
			Hash = newHash;
		}
		
		public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj.GetHashCode());
        }

        private int _hashCode = 0;
        public override int GetHashCode()
        {
            if (_hashCode != 0) return _hashCode;
            // Overflow is fine, just wrap
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Path == null ? 0 : Path.GetHashCode());
                hash = hash * 23 + Directory;
				hash = hash + Path.Length;
                _hashCode = hash;
                return _hashCode;
            }
        }

		public override string ToString()
		{
			return Path;
		}

		private long getContentHash()
		{
			try {
				if (!System.IO.File.Exists(Path))
					return 0;
				var info = new System.IO.FileInfo(Path);
				// Overflow is fine, just wrap
				unchecked
				{
					long hash = 17;
					hash = hash * 23 + info.Length;
					hash = hash * 23 + info.LastWriteTimeUtc.Ticks;
					return hash;
				}
			} catch {
				return 0;
			}
		}
	}
}
