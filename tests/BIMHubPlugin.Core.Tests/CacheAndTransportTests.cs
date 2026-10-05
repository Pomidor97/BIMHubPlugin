using BIMHubPlugin.Services;
using Xunit;

namespace BIMHubPlugin.Core.Tests;

public sealed class CacheAndTransportTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bimhub-cache-test-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTime Version = new(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
    private static Task Write(Stream stream, CancellationToken ct) => stream.WriteAsync(new byte[600_000], ct).AsTask();

    [Theory]
    [InlineData("http://bimhub.kazgor.kz/api", "https://bimhub.kazgor.kz/api")]
    [InlineData("https://example.org/api/", "https://example.org/api")]
    [InlineData("http://localhost:5000/api", "http://localhost:5000/api")]
    public void Normalizes_only_secure_or_loopback_endpoints(string input, string expected)
        => Assert.Equal(expected, ApiEndpoint.Normalize(input));

    [Theory]
    [InlineData("http://example.org/api")]
    [InlineData("https://user:secret@example.org/api")]
    [InlineData("file:///C:/api")]
    [InlineData("https://example.org/api?token=secret")]
    public void Rejects_insecure_or_credential_bearing_endpoints(string input)
        => Assert.Throws<ArgumentException>(() => ApiEndpoint.Normalize(input));

    [Fact]
    public async Task Concurrent_requests_download_once_and_versions_use_different_paths()
    {
        var cache = new CacheService(_folder, 5); var downloads = 0;
        async Task Download(Stream stream, CancellationToken ct) { Interlocked.Increment(ref downloads); await Task.Yield(); await Write(stream, ct); }
        var results = await Task.WhenAll(cache.AcquireAsync("url", Version, ".rfa", Download), cache.AcquireAsync("url", Version, ".rfa", Download));
        Assert.Equal(1, downloads);
        Assert.Equal(results[0].FilePath, results[1].FilePath);
        using var newer = await cache.AcquireAsync("url", Version.AddMinutes(1), ".rfa", Download);
        Assert.NotEqual(results[0].FilePath, newer.FilePath);
        Assert.Equal(2, downloads);
        foreach (var lease in results) lease.Dispose();
    }

    [Fact]
    public async Task Byte_budget_evicts_old_files_and_preserves_active_leases_and_unrelated_files()
    {
        var cache = new CacheService(_folder, 1);
        var unrelated = Path.Combine(_folder, "keep.txt"); await File.WriteAllTextAsync(unrelated, "user file");
        string firstPath;
        using (var first = await cache.AcquireAsync("one", Version, ".rfa", Write))
        {
            firstPath = first.FilePath;
            using var second = await cache.AcquireAsync("two", Version, ".rfa", Write);
            Assert.True(File.Exists(firstPath));
        }
        using var third = await cache.AcquireAsync("three", Version, ".rfa", Write);
        Assert.False(File.Exists(firstPath));
        Assert.True(cache.GetCacheSize() <= 1024 * 1024);
        cache.ClearCache();
        Assert.True(File.Exists(third.FilePath));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public async Task Expired_file_is_refreshed_and_cancelled_download_leaves_no_partial_file()
    {
        var now = DateTime.UtcNow; var downloads = 0;
        var cache = new CacheService(_folder, 5, 1, () => now);
        Task Download(Stream stream, CancellationToken ct) { downloads++; return Write(stream, ct); }
        using (await cache.AcquireAsync("one", Version, ".rfa", Download)) { }
        now = now.AddDays(2);
        using (await cache.AcquireAsync("one", Version, ".rfa", Download)) { }
        Assert.Equal(2, downloads);
        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.AcquireAsync("cancel", Version, ".rfa", async (stream, ct) =>
        { await Write(stream, ct); throw new OperationCanceledException(); }));
        Assert.Empty(Directory.EnumerateFiles(_folder, "*.part"));
    }

    public void Dispose()
    {
        // This fixture owns only its random temp directory, never a user's configured cache.
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
}
