using System;

namespace FSWatcher.FS
{
	class File
	{
		private int _hash = -1;

		public string Path { get; private set; }
		public int Hash {
			get { 
				if (_hash == -1)
					_hash = getContentHash();
				return _hash;
			} 
		}
		public int Directory { get; private set; }

		public File(string file, int dir) {
			Path = file;
			Directory = dir;
		}

		public void SetHash(int newHash)
		{
			_hash = newHash;
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

		private int getContentHash()
		{
			if (!System.IO.File.Exists(Path))
				return 0;
			var info = new System.IO.FileInfo(Path);
			return info.Length.GetHashCode();
		}
	}
}