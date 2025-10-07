using System;
using System.IO;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using BIMHubPlugin.Events;
using BIMHubPlugin.Models;

namespace BIMHubPlugin.Services
{

    public class FamilyLoaderService
    {
        private readonly CatalogApiClient _apiClient;
        private readonly CacheService _cacheService;
        private readonly FamilyLoadExternalEvent _eventHandler;
        private readonly ExternalEvent _externalEvent;

        public FamilyLoaderService(CatalogApiClient apiClient, CacheService cacheService, UIApplication uiApp)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

            _eventHandler = new FamilyLoadExternalEvent();
            _externalEvent = ExternalEvent.Create(_eventHandler);
        }

  
        public async Task LoadFamilyAsync(
        FamilyItem family,
        Action<string> progressCallback,
        Action<bool, string> completionCallback,
        bool showDialog = true)
        {
            try
            {
                SimpleLogger.Log($"LoadFamilyAsync: Starting load for family '{family.Name}' (NameRfa: '{family.NameRfa}')");
                SimpleLogger.Log($"LoadFamilyAsync: Show dialog mode: {showDialog}");
                
                if (string.IsNullOrEmpty(family.DownloadUrl))
                {
                    SimpleLogger.Log($"LoadFamilyAsync: ERROR - DownloadUrl is null or empty");
                    completionCallback?.Invoke(false, $"У семейства '{family.Name}' отсутствует ссылка на файл");
                    return;
                }
                
                progressCallback?.Invoke("Скачивание файла...");

                SimpleLogger.Log($"LoadFamilyAsync: Checking cache for URL: {family.DownloadUrl}");
                string cachedPath = _cacheService.GetCachedFilePath(family.DownloadUrl);

                string localFilePath;

                if (cachedPath != null)
                {
                    localFilePath = cachedPath;
                    SimpleLogger.Log($"LoadFamilyAsync: File found in cache: {localFilePath}");
                    progressCallback?.Invoke("Файл найден в кэше");
                }
                else
                {
                    SimpleLogger.Log("LoadFamilyAsync: File not in cache, downloading...");
                    
                    using (var stream = await _apiClient.DownloadFamilyFileAsync(family.DownloadUrl))
                    {
                        progressCallback?.Invoke("Сохранение файла...");
                        SimpleLogger.Log("LoadFamilyAsync: Download completed, saving to cache...");

                        string extension = Path.GetExtension(family.MainFile);
                        SimpleLogger.Log($"LoadFamilyAsync: File extension: {extension}");
                        
                        localFilePath = await _cacheService.SaveToCacheAsync(
                            family.DownloadUrl,
                            stream,
                            extension
                        );
                        
                        SimpleLogger.Log($"LoadFamilyAsync: File saved to: {localFilePath}");
                    }
                }

                progressCallback?.Invoke("Загрузка в Revit...");
                SimpleLogger.Log($"LoadFamilyAsync: Loading into Revit with file: {localFilePath}");

                _eventHandler.SetLoadData(localFilePath, family.NameRfa, completionCallback, showDialog);
                
                SimpleLogger.Log("LoadFamilyAsync: Raising ExternalEvent...");
                _externalEvent.Raise();
                
                SimpleLogger.Log("LoadFamilyAsync: ExternalEvent raised successfully");
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"LoadFamilyAsync failed for family '{family.Name}'", ex);
                completionCallback?.Invoke(false, $"Ошибка: {ex.Message}");
            }
        }
    }
}