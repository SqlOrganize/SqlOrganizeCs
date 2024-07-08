using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace SqlOrganize.Sql
{
    public class FileSystemCache : IMemoryCache
    {
        private readonly string _cacheDirectory;

        public FileSystemCache(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;
            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }

        public ICacheEntry CreateEntry(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return new FileSystemCacheEntry(GetCacheFilePath(key.ToString()), this);
        }

        /// <summary>Eliminar archivos y directorios de cacheDirectory</summary>
        public void Dispose()
        {
            // Delete all files within the directory
            foreach (string file in Directory.GetFiles(_cacheDirectory))
            {
                File.Delete(file);
            }

            // Delete all subdirectories and their contents
            foreach (string subdirectory in Directory.GetDirectories(_cacheDirectory))
            {
                Directory.Delete(subdirectory, true);
            }
        }

        public void Remove(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            string cacheFilePath = GetCacheFilePath(key.ToString());
            if (File.Exists(cacheFilePath))
                File.Delete(cacheFilePath);
        }

        public bool TryGetValue(object key, out object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            string cacheFilePath = GetCacheFilePath(key.ToString());
            if (File.Exists(cacheFilePath))
            {
                string json = File.ReadAllText(cacheFilePath);
                value = JsonConvert.DeserializeObject(json);
                return true;
            }

            value = null;
            return false;
        }

        private string GetCacheFilePath(string key)
        {
            //string encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            string encodedKey = ValueTypesUtils.Utils.GenerateHash(key);
            return Path.Combine(_cacheDirectory, encodedKey);
        }

        private class FileSystemCacheEntry : ICacheEntry
        {
            private readonly string _cacheFilePath;
            private readonly FileSystemCache _cache;
            private DateTimeOffset? _absoluteExpiration;
            private TimeSpan? _slidingExpiration;
            private object _value;

            public FileSystemCacheEntry(string cacheFilePath, FileSystemCache cache)
            {
                _cacheFilePath = cacheFilePath;
                _cache = cache;
            }

            public object Key => GetKeyFromFilePath(_cacheFilePath);

            public object Value
            {
                get => _value;
                set => _value = value;
            }

            public DateTimeOffset? AbsoluteExpiration
            {
                get => _absoluteExpiration;
                set => _absoluteExpiration = value;
            }

            public TimeSpan? AbsoluteExpirationRelativeToNow
            {
                get => _absoluteExpiration.HasValue ? _absoluteExpiration - DateTimeOffset.UtcNow : null;
                set => _absoluteExpiration = value.HasValue ? DateTimeOffset.UtcNow.Add(value.Value) : null;
            }

            public TimeSpan? SlidingExpiration
            {
                get => _slidingExpiration;
                set => _slidingExpiration = value;
            }

            public IList<IChangeToken> ExpirationTokens => throw new NotImplementedException();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => throw new NotImplementedException();

            public CacheItemPriority Priority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public long? Size { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void Dispose()
            {
                if (Value != null && _cacheFilePath.Length <= 260)
                {
                    string json = JsonConvert.SerializeObject(Value);
                    File.WriteAllText(_cacheFilePath, json);
                }
            }

            public void SetValue(object value)
            {
                Value = value;
            }

            private object GetKeyFromFilePath(string filePath)
            {
                string encodedKey = Path.GetFileNameWithoutExtension(filePath);
                string decodedKey = Encoding.UTF8.GetString(Convert.FromBase64String(encodedKey));
                return decodedKey;
            }
        }
    }

}
