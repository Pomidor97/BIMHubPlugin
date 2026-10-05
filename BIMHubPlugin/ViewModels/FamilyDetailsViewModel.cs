using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BIMHubPlugin.Models;
using BIMHubPlugin.Services;

namespace BIMHubPlugin.ViewModels;

public class FamilyDetailsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
    private bool _disposed;
    public FamilyItem Family { get; }

    public ICommand LoadCommand { get; }
    public ICommand CloseCommand { get; }

    private BitmapImage _previewImage;
    public BitmapImage PreviewImage
    {
        get => _previewImage;
        private set
        {
            _previewImage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewImage)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public FamilyDetailsViewModel(FamilyItem family, CatalogApiClient apiClient, Action closeAction, Func<Task> loadAction)
    {
        Family = family;

        CloseCommand = new RelayCommand(_ => closeAction());
        LoadCommand = new RelayCommand(async _ => await loadAction());

        if (family.HasPreview && !string.IsNullOrEmpty(family.PreviewUrl))
            _ = LoadPreviewAsync(apiClient, family.PreviewUrl);
    }

    // Превью качается через тот же авторизованный HttpClient, что и остальной каталог —
    // прямой Image.Source="{Binding PreviewUrl}" не сработает: у эндпоинта больше нет
    // [AllowAnonymous] (закрытая в BimHelpDesk уязвимость старого BimHub, раздел 3.9 плана).
    private async Task LoadPreviewAsync(CatalogApiClient apiClient, string previewUrl)
    {
        try
        {
            var bytes = await apiClient.DownloadPreviewAsync(previewUrl, _lifetime.Token);
            if (_disposed) return;

            var image = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 1200;
                image.StreamSource = ms;
                image.EndInit();
            }
            image.Freeze();

            PreviewImage = image;
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception ex)
        {
            SimpleLogger.Error($"FamilyDetailsViewModel: preview load failed for '{Family.Name}'", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
