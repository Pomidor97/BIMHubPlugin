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
                SimpleLogger.Log($"GetCategoriesAsync: Requesting {_baseUrl}/Category");
        
                var response = await _httpClient.GetAsync($"{_baseUrl}/Category");
        
                SimpleLogger.Log($"GetCategoriesAsync: Status code {response.StatusCode}");
        
                response.EnsureSuccessStatusCode();
        
                var json = await response.Content.ReadAsStringAsync();
                SimpleLogger.Log($"GetCategoriesAsync: Received JSON length {json.Length}");
        
                var result = JsonConvert.DeserializeObject<List<Category>>(json);
                SimpleLogger.Log($"GetCategoriesAsync: Deserialized {result.Count} categories");
        
                return result;
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetCategoriesAsync failed", ex);
                throw new Exception($"Ошибка получения категорий: {ex.Message}", ex);
            }
        }

        /// <summary>
/// Получить все разделы
/// </summary>
public async Task<List<Section>> GetSectionsAsync()
{
    try
    {
        SimpleLogger.Log($"GetSectionsAsync: Requesting {_baseUrl}/Section");
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/Section");
        
        SimpleLogger.Log($"GetSectionsAsync: Status code {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        SimpleLogger.Log($"GetSectionsAsync: Received JSON length {json.Length}");
        
        var result = JsonConvert.DeserializeObject<List<Section>>(json) ?? new List<Section>();
        SimpleLogger.Log($"GetSectionsAsync: Deserialized {result.Count} sections");
        
        return result;
    }
    catch (Exception ex)
    {
        SimpleLogger.Error("GetSectionsAsync failed", ex);
        throw new Exception($"Ошибка получения разделов: {ex.Message}", ex);
    }
}

/// <summary>
/// Получить производителей
/// </summary>
public async Task<List<Manufacturer>> GetManufacturersAsync()
{
    try
    {
        SimpleLogger.Log($"GetManufacturersAsync: Requesting {_baseUrl}/Manufacturer");
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/Manufacturer");
        
        SimpleLogger.Log($"GetManufacturersAsync: Status code {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        SimpleLogger.Log($"GetManufacturersAsync: Received JSON length {json.Length}");
        
        var result = JsonConvert.DeserializeObject<List<Manufacturer>>(json) ?? new List<Manufacturer>();
        SimpleLogger.Log($"GetManufacturersAsync: Deserialized {result.Count} manufacturers");
        
        return result;
    }
    catch (Exception ex)
    {
        SimpleLogger.Error("GetManufacturersAsync failed", ex);
        throw new Exception($"Ошибка получения производителей: {ex.Message}", ex);
    }
}

/// <summary>
/// Получить версии Revit
/// </summary>
public async Task<List<RevitVersion>> GetRevitVersionsAsync()
{
    try
    {
        SimpleLogger.Log($"GetRevitVersionsAsync: Requesting {_baseUrl}/RevitVersion");
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/RevitVersion");
        
        SimpleLogger.Log($"GetRevitVersionsAsync: Status code {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        SimpleLogger.Log($"GetRevitVersionsAsync: Received JSON length {json.Length}");
        
        var result = JsonConvert.DeserializeObject<List<RevitVersion>>(json) ?? new List<RevitVersion>();
        SimpleLogger.Log($"GetRevitVersionsAsync: Deserialized {result.Count} versions");
        
        return result;
    }
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
                SimpleLogger.Log("GetFamiliesAsync: Building query parameters");
                
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
                string url = $"{_baseUrl}/Family?{query}";
                
                SimpleLogger.Log($"GetFamiliesAsync: Requesting {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                SimpleLogger.Log($"GetFamiliesAsync: Status code {response.StatusCode}");
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                SimpleLogger.Log($"GetFamiliesAsync: Received JSON length {json.Length}");
                
                SimpleLogger.Log($"GetFamiliesAsync: JSON preview: {json.Substring(0, Math.Min(500, json.Length))}");
                
                var result = JsonConvert.DeserializeObject<PagedResult<FamilyItem>>(json);
                
                SimpleLogger.Log($"GetFamiliesAsync: Deserialized {result.Items.Count} items, Total: {result.TotalCount}");
                
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
                
                SimpleLogger.Log("GetFamiliesAsync: URLs updated successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetFamiliesAsync failed", ex);
                throw new Exception($"Ошибка получения семейств: {ex.Message}", ex);
            }
        }

        public async Task<FamilyItem> GetFamilyByIdAsync(Guid id)
        {
            try
            {
                string url = $"{_baseUrl}/Family/{id}";
                
                SimpleLogger.Log($"GetFamilyByIdAsync: Requesting {url}");
                
                var response = await _httpClient.GetAsync(url);
                
                SimpleLogger.Log($"GetFamilyByIdAsync: Status code {response.StatusCode}");
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                SimpleLogger.Log($"GetFamilyByIdAsync: Received JSON length {json.Length}");
                
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
                
                SimpleLogger.Log($"GetFamilyByIdAsync: Successfully loaded family '{item.Name}'");
                
                return item;
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"GetFamilyByIdAsync failed for ID {id}", ex);
                throw new Exception($"Ошибка получения семейства: {ex.Message}", ex);
            }
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