using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BIMHubPlugin.Services
{
    public sealed class CacheService
    {
        private readonly string _root;
        private readonly long _maxBytes;
        private readonly TimeSpan _ttl;
        private readonly Func<DateTime> _utcNow;
        private static readonly SemaphoreSlim IoGate = new SemaphoreSlim(1);
        private static readonly object LeaseLock = new object();
        private static readonly Dictionary<string, int> Leases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex OwnedName = new Regex(@"^[a-f0-9]{64}\.(rfa|rvt|rte|dat)$", RegexOptions.Compiled);

        public CacheService(string cacheFolder, long maxCacheSizeMB = 500, int ttlDays = 7, Func<DateTime> utcNow = null)
        {
            _root = Path.GetFullPath(cacheFolder ?? throw new ArgumentNullException(nameof(cacheFolder)));
            _maxBytes = checked(Math.Max(1, maxCacheSizeMB) * 1024 * 1024);
            _ttl = TimeSpan.FromDays(Math.Max(1, ttlDays));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            Directory.CreateDirectory(_root);
        }

        public async Task<CachedFileLease> AcquireAsync(string url, DateTime version, string extension,
            Func<Stream, CancellationToken, Task> download, CancellationToken ct = default)
        {
            extension = (extension ?? ".rfa").ToLowerInvariant();
            if (extension != ".rfa" && extension != ".rvt" && extension != ".rte" && extension != ".dat")
                throw new ArgumentException("Неподдерживаемое расширение файла кэша.");
            string key;
            using (var hash = SHA256.Create())
                key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(url + "|" + version.ToUniversalTime().Ticks))).Replace("-", "").ToLowerInvariant();
            var path = Path.Combine(_root, key + extension);
            await IoGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();
                var fresh = File.Exists(path) && new FileInfo(path).Length > 0 &&
                    (_utcNow() - File.GetLastWriteTimeUtc(path) <= _ttl || IsLeased(path));
                if (!fresh)
                {
                    var temporary = path + "." + Guid.NewGuid().ToString("N") + ".part";
                    try
                    {
                        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                        {
                            await download(stream, ct).ConfigureAwait(false);
                            if (stream.Length == 0) throw new IOException("Сервер вернул пустой файл.");
                            if (stream.Length > _maxBytes) throw new IOException("Размер семейства превышает размер кэша.");
                            await stream.FlushAsync(ct).ConfigureAwait(false);
                        }
                        ct.ThrowIfCancellationRequested();
                        if (File.Exists(path)) File.Delete(path);
                        File.Move(temporary, path);
                        File.SetLastWriteTimeUtc(path, _utcNow());
                    }
                    finally
                    {
                        if (File.Exists(temporary)) File.Delete(temporary);
                    }
                }
                File.SetLastAccessTimeUtc(path, _utcNow());
                lock (LeaseLock) Leases[path] = Leases.TryGetValue(path, out var count) ? count + 1 : 1;
                try { Trim(path); }
                catch { Release(path); throw; }
                return new CachedFileLease(path, () => Release(path));
            }
            finally { IoGate.Release(); }
        }

        private static bool IsLeased(string path)
        {
            lock (LeaseLock) return Leases.ContainsKey(path);
        }
        private static void Release(string path)
        {
            lock (LeaseLock)
            {
                if (!Leases.TryGetValue(path, out var count)) return;
                if (count == 1) Leases.Remove(path); else Leases[path] = count - 1;
            }
        }
        private IEnumerable<FileInfo> Files() => new DirectoryInfo(_root).EnumerateFiles()
            .Where(f => OwnedName.IsMatch(f.Name) && (f.Attributes & FileAttributes.ReparsePoint) == 0);
        public long GetCacheSize() => Files().Sum(f => f.Length);
        private void Trim(string protectedPath)
        {
            var files = Files().OrderBy(f => f.LastAccessTimeUtc).ToList();
            var size = files.Sum(f => f.Length);
            foreach (var file in files)
            {
                if (file.FullName == protectedPath || IsLeased(file.FullName)) continue;
                if (size <= _maxBytes && _utcNow() - file.LastWriteTimeUtc <= _ttl) continue;
                try { var length = file.Length; file.Delete(); size -= length; }
                catch (IOException) { /* Another Revit process may currently use the file. */ }
                catch (UnauthorizedAccessException) { /* Keep a file we cannot safely delete. */ }
            }
        }
        public void ClearCache()
        {
            IoGate.Wait();
            try
            {
                foreach (var file in Files())
                {
                    if (IsLeased(file.FullName)) continue;
                    try { file.Delete(); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            finally { IoGate.Release(); }
        }
    }

    public sealed class CachedFileLease : IDisposable
    {
        private Action _release;
        public string FilePath { get; }
        internal CachedFileLease(string filePath, Action release) { FilePath = filePath; _release = release; }
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
