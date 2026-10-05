using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BIMHubPlugin.Models;
using Newtonsoft.Json;

namespace BIMHubPlugin.Services
{
    public sealed class CatalogApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly SemaphoreSlim _previews = new SemaphoreSlim(4);
        public CatalogApiClient(string baseUrl, string apiToken = null, int timeoutSeconds = 300)
        {
            _baseUrl = ApiEndpoint.Normalize(baseUrl);
            _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(5, Math.Min(timeoutSeconds, 300))),
                MaxResponseContentBufferSize = 16 * 1024 * 1024
            };
            if (!string.IsNullOrEmpty(apiToken))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }
        private void ValidateDownloadUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                !uri.AbsoluteUri.StartsWith(_baseUrl + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("Ссылка на файл находится за пределами настроенного API.");
        }
        private static void EnsureSuccess(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Сессия истекла или отозвана. Выполните вход повторно.");
            response.EnsureSuccessStatusCode();
        }
        private async Task<T> GetAsync<T>(string path, CancellationToken ct)
        {
            using (var response = await _httpClient.GetAsync(_baseUrl + path, ct).ConfigureAwait(false))
            {
                EnsureSuccess(response);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
        public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
            => await GetAsync<List<Category>>("/catalog/categories", ct).ConfigureAwait(false) ?? new List<Category>();
        public async Task<List<Section>> GetSectionsAsync(CancellationToken ct = default)
            => await GetAsync<List<Section>>("/catalog/sections", ct).ConfigureAwait(false) ?? new List<Section>();
        public async Task<List<Manufacturer>> GetManufacturersAsync(CancellationToken ct = default)
            => await GetAsync<List<Manufacturer>>("/catalog/manufacturers", ct).ConfigureAwait(false) ?? new List<Manufacturer>();
        public async Task<List<RevitVersion>> GetRevitVersionsAsync(CancellationToken ct = default)
            => await GetAsync<List<RevitVersion>>("/catalog/revit-versions", ct).ConfigureAwait(false) ?? new List<RevitVersion>();

        public async Task<PagedResult<FamilyItem>> GetFamiliesAsync(FilterOptions filter, CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter.Search)) query.Add("search=" + Uri.EscapeDataString(filter.Search));
            if (filter.CategoryId.HasValue) query.Add("categoryId=" + filter.CategoryId);
            if (filter.ManufacturerId.HasValue) query.Add("manufacturerId=" + filter.ManufacturerId);
            if (filter.RevitVersionId.HasValue) query.Add("revitVersionId=" + filter.RevitVersionId);
            if (filter.SectionId.HasValue) query.Add("sectionId=" + filter.SectionId);
            query.Add("sortBy=" + Uri.EscapeDataString(filter.SortBy ?? "name"));
            query.Add("sortOrder=" + Uri.EscapeDataString(filter.SortOrder ?? "asc"));
            query.Add("page=" + Math.Max(1, filter.Page));
            query.Add("pageSize=" + Math.Max(1, Math.Min(100, filter.PageSize)));
            var result = await GetAsync<PagedResult<FamilyItem>>("/catalog/families?" + string.Join("&", query), ct).ConfigureAwait(false)
                ?? new PagedResult<FamilyItem>();
            result.Items = result.Items ?? new List<FamilyItem>();
            foreach (var item in result.Items) SetUrls(item);
            await Task.WhenAll(result.Items.Where(i => i.HasPreview).Select(i => LoadPreviewAsync(i, ct))).ConfigureAwait(false);
            return result;
        }
        public async Task<FamilyItem> GetFamilyByIdAsync(Guid id, CancellationToken ct = default)
        {
            var item = await GetAsync<FamilyItem>("/catalog/families/" + id, ct).ConfigureAwait(false);
            if (item == null) throw new InvalidOperationException("Семейство не найдено.");
            SetUrls(item);
            return item;
        }
        private void SetUrls(FamilyItem item)
        {
            item.DownloadUrl = _baseUrl + "/catalog/families/" + item.Id + "/download";
            item.PreviewUrl = item.HasPreview ? _baseUrl + "/catalog/families/" + item.Id + "/preview" : null;
        }
        private async Task LoadPreviewAsync(FamilyItem item, CancellationToken ct)
        {
            await _previews.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var bytes = await DownloadPreviewAsync(item.PreviewUrl, ct).ConfigureAwait(false);
                using (var stream = new MemoryStream(bytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = 320;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    item.PreviewImage = image;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex) { SimpleLogger.Error("Cannot load preview " + item.Id, ex); }
            finally { _previews.Release(); }
        }
        public async Task<byte[]> DownloadPreviewAsync(string url, CancellationToken ct = default)
        {
            ValidateDownloadUrl(url);
            using (var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false))
            {
                EnsureSuccess(response);
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }
        public async Task DownloadToAsync(string url, Stream destination, CancellationToken ct = default)
        {
            ValidateDownloadUrl(url);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(_httpClient.Timeout);
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    EnsureSuccess(response);
                    using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        await source.CopyToAsync(destination, 81920, timeout.Token).ConfigureAwait(false);
                }
            }
        }
        public async Task LogoutAsync(CancellationToken ct = default)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                using (var response = await _httpClient.PostAsync(_baseUrl + "/auth/logout", null, timeout.Token).ConfigureAwait(false))
                    EnsureSuccess(response);
            }
        }
        public void Dispose()
        {
            // In-flight preview tasks release their permits after cancellation.
            _httpClient.Dispose();
        }
    }
}
