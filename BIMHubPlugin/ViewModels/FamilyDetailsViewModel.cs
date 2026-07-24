using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BIMHubPlugin.Models;
using BIMHubPlugin.Services;

namespace BIMHubPlugin.ViewModels;

public class FamilyDetailsViewModel : INotifyPropertyChanged
{
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

    public FamilyDetailsViewModel(FamilyItem family, CatalogApiClient apiClient, Action closeAction, Action loadAction)
    {
        Family = family;

        CloseCommand = new RelayCommand(_ => closeAction());
        LoadCommand = new RelayCommand(_ => loadAction());

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
            var bytes = await apiClient.DownloadPreviewAsync(previewUrl);

            var image = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            image.Freeze();

            PreviewImage = image;
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"FamilyDetailsViewModel: preview load failed for '{Family.Name}'", ex);
        }
    }
}
