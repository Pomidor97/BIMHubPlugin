using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BIMHubPlugin.Models;
using Newtonsoft.Json;

namespace BIMHubPlugin.Services
{
    /// <summary>
    /// Клиент нового Catalog API BimHelpDesk (модуль Catalog, замена прежнего BimHub API —
    /// раздел 7.1/9 плана объединения). Все эндпоинты защищены правом catalog.view — в отличие
    /// от старого BimHub, Preview/Download больше не [AllowAnonymous], поэтому и превью,
    /// и файлы качаются только через этот клиент с Bearer-токеном, никогда напрямую из XAML.
    /// </summary>
    public class CatalogApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public CatalogApiClient(string baseUrl, string apiToken = null)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromMinutes(5)
            };

            if (!string.IsNullOrEmpty(apiToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiToken);
            }
        }

        /// <summary>Бросает UnauthorizedAccessException на 401 — вызывающий код перелогинивает пользователя.</summary>
        private static void EnsureAuthorized(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Сессия истекла или недействительна, требуется повторный вход");
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/catalog/categories");
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Category>>(json) ?? new List<Category>();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetCategoriesAsync failed", ex);
                throw new Exception($"Ошибка получения категорий: {ex.Message}", ex);
            }
        }

        public async Task<List<Section>> GetSectionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/catalog/sections");
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Section>>(json) ?? new List<Section>();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetSectionsAsync failed", ex);
                throw new Exception($"Ошибка получения разделов: {ex.Message}", ex);
            }
        }

        public async Task<List<Manufacturer>> GetManufacturersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/catalog/manufacturers");
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Manufacturer>>(json) ?? new List<Manufacturer>();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetManufacturersAsync failed", ex);
                throw new Exception($"Ошибка получения производителей: {ex.Message}", ex);
            }
        }

        public async Task<List<RevitVersion>> GetRevitVersionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/catalog/revit-versions");
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<RevitVersion>>(json) ?? new List<RevitVersion>();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetRevitVersionsAsync failed", ex);
                throw new Exception($"Ошибка получения версий Revit: {ex.Message}", ex);
            }
        }

        public async Task<PagedResult<FamilyItem>> GetFamiliesAsync(FilterOptions filter)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(filter.Search))
                    queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");
                if (filter.CategoryId.HasValue)
                    queryParams.Add($"categoryId={filter.CategoryId.Value}");
                if (filter.ManufacturerId.HasValue)
                    queryParams.Add($"manufacturerId={filter.ManufacturerId.Value}");
                if (filter.RevitVersionId.HasValue)
                    queryParams.Add($"revitVersionId={filter.RevitVersionId.Value}");
                if (filter.SectionId.HasValue)
                    queryParams.Add($"sectionId={filter.SectionId.Value}");

                queryParams.Add($"sortBy={filter.SortBy}");
                queryParams.Add($"sortOrder={filter.SortOrder}");
                queryParams.Add($"page={filter.Page}");
                queryParams.Add($"pageSize={filter.PageSize}");

                string query = string.Join("&", queryParams);
                string url = $"{_baseUrl}/catalog/families?{query}";

                SimpleLogger.Log($"GetFamiliesAsync: Requesting {url}");

                var response = await _httpClient.GetAsync(url);
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PagedResult<FamilyItem>>(json)
                             ?? new PagedResult<FamilyItem> { Items = new List<FamilyItem>() };

                if (result.Items == null)
                    result.Items = new List<FamilyItem>();

                foreach (var item in result.Items)
                {
                    item.DownloadUrl = $"{_baseUrl}/catalog/families/{item.Id}/download";
                    item.PreviewUrl = item.HasPreview ? $"{_baseUrl}/catalog/families/{item.Id}/preview" : null;
                }

                // Превью защищено Bearer-токеном — прямой Image.Source="{Binding PreviewUrl}"
                // в списке карточек не сработает, поэтому качаем миниатюры сразу (страница
                // небольшая, обычно 12 штук) и кладём готовые BitmapImage в модель.
                await Task.WhenAll(result.Items
                    .Where(i => i.HasPreview && !string.IsNullOrEmpty(i.PreviewUrl))
                    .Select(LoadPreviewImageAsync));

                SimpleLogger.Log($"GetFamiliesAsync: Deserialized {result.Items.Count} items, Total: {result.TotalCount}");
                return result;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetFamiliesAsync failed", ex);
                throw new Exception($"Ошибка получения семейств: {ex.Message}", ex);
            }
        }

        private async Task LoadPreviewImageAsync(FamilyItem item)
        {
            try
            {
                var bytes = await DownloadPreviewAsync(item.PreviewUrl);

                var image = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                image.Freeze(); // созданo вне UI-потока — фиксируем, чтобы можно было отдать в биндинг

                item.PreviewImage = image;
            }
            catch (Exception ex)
            {
                // Не проваливаем всю страницу из-за одной сломанной миниатюры.
                SimpleLogger.Error($"LoadPreviewImageAsync failed for '{item.Name}'", ex);
            }
        }

        public async Task<FamilyItem> GetFamilyByIdAsync(Guid id)
        {
            try
            {
                string url = $"{_baseUrl}/catalog/families/{id}";
                var response = await _httpClient.GetAsync(url);
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var item = JsonConvert.DeserializeObject<FamilyItem>(json);

                item.DownloadUrl = $"{_baseUrl}/catalog/families/{item.Id}/download";
                item.PreviewUrl = item.HasPreview ? $"{_baseUrl}/catalog/families/{item.Id}/preview" : null;

                SimpleLogger.Log($"GetFamilyByIdAsync: Successfully loaded family '{item.Name}'");
                return item;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error($"GetFamilyByIdAsync failed for ID {id}", ex);
                throw new Exception($"Ошибка получения семейства: {ex.Message}", ex);
            }
        }

        /// <summary>Скачать основной файл семейства (.rfa) — считается на сервере атомарно (download_count).</summary>
        public async Task<Stream> DownloadFamilyFileAsync(string downloadUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error($"DownloadFamilyFileAsync failed for URL: {downloadUrl}", ex);
                throw new Exception($"Ошибка скачивания файла: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Скачать превью изображение. Эндпоинт защищён (в отличие от старого BimHub, где
        /// FilesController.Preview был [AllowAnonymous]) — поэтому картинку больше нельзя
        /// грузить прямой WPF-биндингом Image.Source на URL, только так, через Bearer-токен.
        /// </summary>
        public async Task<byte[]> DownloadPreviewAsync(string previewUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(previewUrl);
                EnsureAuthorized(response);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error($"DownloadPreviewAsync failed for URL: {previewUrl}", ex);
                throw new Exception($"Ошибка скачивания превью: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
