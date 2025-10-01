using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BIMHubPlugin.Services
{
    public class CacheService
    {
        private readonly string _cacheFolder;
        private readonly long _maxCacheSizeBytes;
        private readonly ConcurrentDictionary<string, CacheEntry> _cacheIndex;

        public CacheService(string cacheFolder = null, long maxCacheSizeMB = 500)
        {
            _cacheFolder = cacheFolder ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BIMHubPlugin",
                "Cache"
            );

            _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;
            _cacheIndex = new ConcurrentDictionary<string, CacheEntry>();

            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }

            LoadCacheIndex();
        }

        public string GetCachedFilePath(string url)
        {
            string key = GetCacheKey(url);
            
            if (_cacheIndex.TryGetValue(key, out var entry))
            {
                entry.LastAccessed = DateTime.UtcNow;
                
                if (File.Exists(entry.FilePath))
                {
                    return entry.FilePath;
                }
                else
                {
                    _cacheIndex.TryRemove(key, out _);
                }
            }

            return null;
        }

        public async Task<string> SaveToCacheAsync(string url, byte[] data, string extension = null)
        {
            string key = GetCacheKey(url);
            string fileName = key + (extension ?? ".dat");
            string filePath = Path.Combine(_cacheFolder, fileName);

            // .NET Framework 4.8 - используем синхронную версию
            File.WriteAllBytes(filePath, data);

            var entry = new CacheEntry
            {
                Key = key,
                FilePath = filePath,
                FileSize = data.Length,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _cacheIndex[key] = entry;

            await EnforceCacheLimitAsync();

            return filePath;
        }

        public async Task<string> SaveToCacheAsync(string url, Stream stream, string extension = null)
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return await SaveToCacheAsync(url, ms.ToArray(), extension);
            }
        }

        public void ClearCache()
        {
            foreach (var entry in _cacheIndex.Values)
            {
                try
                {
                    if (File.Exists(entry.FilePath))
                    {
                        File.Delete(entry.FilePath);
                    }
                }
                catch { }
            }

            _cacheIndex.Clear();
        }

        public long GetCacheSize()
        {
            return _cacheIndex.Values.Sum(e => e.FileSize);
        }

        private string GetCacheKey(string url)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void LoadCacheIndex()
        {
            if (!Directory.Exists(_cacheFolder))
                return;

            foreach (var filePath in Directory.GetFiles(_cacheFolder))
            {
                var fileInfo = new FileInfo(filePath);
                string key = Path.GetFileNameWithoutExtension(filePath);

                var entry = new CacheEntry
                {
                    Key = key,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    LastAccessed = fileInfo.LastAccessTimeUtc
                };

                _cacheIndex[key] = entry;
            }
        }

        private async Task EnforceCacheLimitAsync()
        {
            long currentSize = GetCacheSize();

            if (currentSize <= _maxCacheSizeBytes)
                return;

            var entriesToRemove = _cacheIndex.Values
                .OrderBy(e => e.LastAccessed)
                .Take((int)(currentSize * 0.2 / 1024 / 1024))
                .ToList();

            foreach (var entry in entriesToRemove)
            {
                try
                {
                    if (File.Exists(entry.FilePath))
                    {
                        File.Delete(entry.FilePath);
                    }
                    _cacheIndex.TryRemove(entry.Key, out _);
                }
                catch { }
            }

            await Task.CompletedTask;
        }

        private class CacheEntry
        {
            public string Key { get; set; }
            public string FilePath { get; set; }
            public long FileSize { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccessed { get; set; }
        }
    }
}