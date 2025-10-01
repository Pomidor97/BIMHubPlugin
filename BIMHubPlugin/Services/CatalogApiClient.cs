using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BIMHubPlugin.Models;
using Newtonsoft.Json;

namespace BIMHubPlugin.Services
{
    /// <summary>
    /// Клиент для работы с BIMHubPlugin API
    /// </summary>
    public class CatalogApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiToken;

        public CatalogApiClient(string baseUrl, string apiToken = null)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _apiToken = apiToken;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromMinutes(5)
            };

            // Добавляем Bearer токен если есть
            if (!string.IsNullOrEmpty(_apiToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _apiToken);
            }
        }

        /// <summary>
        /// Получить все категории
        /// </summary>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Category");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Category>>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения категорий: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получить все разделы
        /// </summary>
        public async Task<List<Section>> GetSectionsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/Section");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Section>>(json);
        }

        /// <summary>
        /// Получить производителей
        /// </summary>
        public async Task<List<Manufacturer>> GetManufacturersAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/Manufacturer");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Manufacturer>>(json);
        }

        /// <summary>
        /// Получить версии Revit
        /// </summary>
        public async Task<List<RevitVersion>> GetRevitVersionsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/RevitVersion");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RevitVersion>>(json);
        }

        /// <summary>
        /// Получить список семейств с фильтрацией и пагинацией
        /// </summary>
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
                string url = $"/api/Family?{query}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PagedResult<FamilyItem>>(json);

                // Дополняем URL для превью и скачивания
                foreach (var item in result.Items)
                {
                    if (!string.IsNullOrEmpty(item.PreviewFile))
                    {
                        item.PreviewUrl = $"http://bimhub.kazgor.kz:5058/api/files/preview/{item.PreviewFile}";
                    }
                    if (!string.IsNullOrEmpty(item.MainFile))
                    {
                        item.DownloadUrl = $"http://bimhub.kazgor.kz:5058/api/files/download/{item.MainFile}";
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения семейств: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получить детали семейства по ID
        /// </summary>
        public async Task<FamilyItem> GetFamilyByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"/api/Family/{id}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var item = JsonConvert.DeserializeObject<FamilyItem>(json);

            // Дополняем URL
            if (!string.IsNullOrEmpty(item.PreviewFile))
            {
                item.PreviewUrl = $"{_baseUrl}/api/files/preview/{item.PreviewFile}";
            }
            if (!string.IsNullOrEmpty(item.MainFile))
            {
                item.DownloadUrl = $"{_baseUrl}/api/files/download/{item.MainFile}";
            }

            return item;
        }

        /// <summary>
        /// Скачать файл семейства (.rfa)
        /// </summary>
        public async Task<Stream> DownloadFamilyFileAsync(string downloadUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка скачивания файла: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Скачать превью изображение
        /// </summary>
        public async Task<byte[]> DownloadPreviewAsync(string previewUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(previewUrl);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка скачивания превью: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}