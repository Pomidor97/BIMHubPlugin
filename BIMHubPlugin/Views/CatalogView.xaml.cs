using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using BIMHubPlugin.Services;
using BIMHubPlugin.ViewModels;

namespace BIMHubPlugin.Views
{
    public partial class CatalogView : UserControl
    {
        private UIApplication _uiApp;

        public CatalogView()
        {
            InitializeComponent();
        }

        public void SetUIApplication(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            try
            {
                if (_uiApp == null)
                {
                    MessageBox.Show(
                        "UIApplication не инициализирован",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                var config = Services.ConfigService.LoadConfig();
                var apiClient = new Services.CatalogApiClient(config.ApiBaseUrl, config.ApiToken);
                var cacheService = new Services.CacheService(Services.ConfigService.GetCacheFolder(), config.CacheSizeMB);
                var loaderService = new Services.FamilyLoaderService(apiClient, cacheService, _uiApp);
        
                // Передаем Dispatcher из View
                var viewModel = new ViewModels.CatalogViewModel(
                    apiClient, 
                    loaderService, 
                    cacheService, 
                    this.Dispatcher  // Передаем Dispatcher
                );

                DataContext = viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка инициализации каталога:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}