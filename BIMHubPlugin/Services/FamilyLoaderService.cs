using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using BIMHubPlugin.Models;

namespace BIMHubPlugin.Services
{
    public sealed class FamilyLoaderService
    {
        private readonly CatalogApiClient _apiClient;
        private readonly CacheService _cache;
        private readonly SemaphoreSlim _loading = new SemaphoreSlim(1);
        public FamilyLoaderService(CatalogApiClient apiClient, CacheService cache, UIApplication uiApp)
        { _apiClient = apiClient; _cache = cache; }

        public async Task LoadFamilyAsync(FamilyItem family, Action<string> progressCallback,
            Action<bool, string> completionCallback, bool showDialog = true, CancellationToken ct = default)
        {
            if (!await _loading.WaitAsync(0, ct)) return;
            try
            {
                var targetDocument = App.ActiveDocument;
                if (targetDocument == null) throw new InvalidOperationException("Нет активного документа Revit.");
                progressCallback?.Invoke("Проверка актуальной версии семейства...");
                // Always recheck access and fetch the real file extension/version, even on a cache hit.
                var detail = await _apiClient.GetFamilyByIdAsync(family.Id, ct);
                var extension = Path.GetExtension(detail.MainFileDisplayName ?? "").ToLowerInvariant();
                if (extension != ".rfa")
                    throw new InvalidOperationException("В проект можно загрузить только .rfa. Файлы .rvt/.rte открываются как документы Revit.");
                progressCallback?.Invoke("Скачивание файла...");
                using (var lease = await _cache.AcquireAsync(detail.DownloadUrl, detail.UpdatedAt, extension,
                    (stream, token) => _apiClient.DownloadToAsync(detail.DownloadUrl, stream, token), ct))
                {
                    ct.ThrowIfCancellationRequested();
                    progressCallback?.Invoke("Загрузка в Revit...");
                    var result = await App.FamilyLoads.LoadAsync(lease.FilePath, detail.NameRfa, targetDocument, showDialog, ct);
                    completionCallback?.Invoke(result.Success, result.Message);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                SimpleLogger.Error("Family load failed", ex);
                completionCallback?.Invoke(false, ex.Message);
            }
            finally { _loading.Release(); }
        }
    }
}
