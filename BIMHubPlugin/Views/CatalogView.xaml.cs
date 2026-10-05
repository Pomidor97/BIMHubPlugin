using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using BIMHubPlugin.Services;

namespace BIMHubPlugin.Views
{
    public partial class CatalogView : UserControl, IDisposable
    {
        private UIApplication _uiApp;
        private CatalogApiClient _apiClient;
        private bool _initializing, _reloginQueued, _disposed;

        public CatalogView()
        {
            InitializeComponent();
        }

        public void SetUIApplication(UIApplication uiApp)
        {
            _uiApp = uiApp;
            if (DataContext == null) InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            if (_initializing || _disposed) return;
            _initializing = true;
            try
            {
                DisposeCurrentSession();
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

                var token = ResolveToken(config.ApiBaseUrl);
                if (token == null)
                {
                    // Пользователь отменил окно логина — оставляем панель пустой,
                    // а не падаем с ошибкой "не удалось загрузить каталог".
                    DataContext = null;
                    return;
                }

                var apiClient = new Services.CatalogApiClient(config.ApiBaseUrl, token, config.RequestTimeoutSeconds);
                _apiClient = apiClient;
                var cacheService = new Services.CacheService(Services.ConfigService.GetCacheFolder(), config.CacheSizeMB, config.CacheTTLDays);
                var loaderService = new Services.FamilyLoaderService(apiClient, cacheService, _uiApp);

                var viewModel = new ViewModels.CatalogViewModel(
                    apiClient,
                    loaderService,
                    cacheService,
                    this.Dispatcher,
                    onUnauthorized: () =>
                    {
                        // Токен истёк/отозван (401) ИЛИ пользователь сам нажал "Выход" —
                        // в обоих случаях один и тот же эффект: чистим кэш, просим войти заново.
                        if (_disposed || _reloginQueued) return;
                        _reloginQueued = true;
                        Services.TokenStore.Clear();
                        this.Dispatcher.BeginInvoke(new Action(() => { _reloginQueued = false; InitializeViewModel(); }));
                    },
                    pageSize: config.DefaultPageSize
                );

                DataContext = viewModel;
                _ = viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                DisposeCurrentSession();
                MessageBox.Show(
                    $"Ошибка инициализации каталога:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally { _initializing = false; }
        }

        private void DisposeCurrentSession()
        {
            (DataContext as IDisposable)?.Dispose();
            DataContext = null;
            _apiClient?.Dispose();
            _apiClient = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DisposeCurrentSession();
        }

        /// <summary>
        /// Токен из локального DPAPI-кэша (если относится к текущему ApiBaseUrl), иначе —
        /// диалог логина через AD (раздел 7.2/10 плана: полноценный логин вместо ручного
        /// редактирования токена в открытом config.json). Null — пользователь отменил вход.
        /// </summary>
        private string ResolveToken(string apiBaseUrl)
        {
            var cached = Services.TokenStore.Load();
            if (cached != null
                && string.Equals(cached.ApiBaseUrl, apiBaseUrl, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(cached.Token))
            {
                return cached.Token;
            }

            var owner = Application.Current?.MainWindow;
            var login = new LoginWindow(apiBaseUrl);
            if (owner != null && !ReferenceEquals(owner, login))
                login.Owner = owner;

            var ok = login.ShowDialog();
            return ok == true ? login.Result.Token : null;
        }
    }
}
