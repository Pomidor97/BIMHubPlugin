using System;
using System.IO;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using BIMHubPlugin.Events;
using BIMHubPlugin.Models;

namespace BIMHubPlugin.Services
{
    /// <summary>
    /// Сервис для загрузки семейств (координирует скачивание и импорт в Revit)
    /// </summary>
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

            // Создаем IExternalEvent для потокобезопасной загрузки в Revit
            _eventHandler = new FamilyLoadExternalEvent();
            _externalEvent = ExternalEvent.Create(_eventHandler);
        }

        /// <summary>
        /// Загрузить семейство: скачать файл и импортировать в Revit
        /// </summary>
        /// <param name="family">Элемент каталога</param>
        /// <param name="progressCallback">Callback для отображения прогресса</param>
        /// <param name="completionCallback">Callback завершения (success, message)</param>
        public async Task LoadFamilyAsync(
            FamilyItem family,
            Action<string> progressCallback,
            Action<bool, string> completionCallback)
        {
            try
            {
                progressCallback?.Invoke("Скачивание файла...");

                // 1. Проверяем кэш
                string cachedPath = _cacheService.GetCachedFilePath(family.DownloadUrl);

                string localFilePath;

                if (cachedPath != null)
                {
                    localFilePath = cachedPath;
                    progressCallback?.Invoke("Файл найден в кэше");
                }
                else
                {
                    // 2. Скачиваем файл
                    using (var stream = await _apiClient.DownloadFamilyFileAsync(family.DownloadUrl))
                    {
                        progressCallback?.Invoke("Сохранение файла...");

                        // Сохраняем в кэш
                        string extension = Path.GetExtension(family.MainFile);
                        localFilePath = await _cacheService.SaveToCacheAsync(
                            family.DownloadUrl,
                            stream,
                            extension
                        );
                    }
                }

                progressCallback?.Invoke("Загрузка в Revit...");

                // 3. Загружаем в Revit через IExternalEvent (в главном потоке!)
                _eventHandler.SetLoadData(localFilePath, completionCallback);
                _externalEvent.Raise(); // Вызываем событие - выполнится в главном потоке Revit
            }
            catch (Exception ex)
            {
                completionCallback?.Invoke(false, $"Ошибка: {ex.Message}");
            }
        }
    }
}