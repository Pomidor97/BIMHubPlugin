using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BIMHubPlugin.Models;
using BIMHubPlugin.Services;
using BIMHubPlugin.Views;

namespace BIMHubPlugin.ViewModels
{
    public sealed class CatalogViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CatalogApiClient _apiClient;
        private readonly FamilyLoaderService _loaderService;
        private readonly Action _onUnauthorized;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly HashSet<FamilyDetailsWindow> _details = new HashSet<FamilyDetailsWindow>();
        private CancellationTokenSource _request;
        private bool _disposed, _suspendSearch, _isFamilyLoading, _searchPending;
        private string _searchText, _statusMessage;
        private Category _selectedCategory;
        private Section _selectedSection;
        private Manufacturer _selectedManufacturer;
        private RevitVersion _selectedRevitVersion;
        private ObservableCollection<FamilyItem> _families;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Section> _sections;
        private ObservableCollection<Manufacturer> _manufacturers;
        private ObservableCollection<RevitVersion> _revitVersions;
        private int _currentPage = 1, _pageSize, _totalPages, _totalCount;
        private bool _isLoading;
        private FamilyItem _selectedFamily;

        public CatalogViewModel(CatalogApiClient apiClient, FamilyLoaderService loaderService, CacheService cacheService,
            System.Windows.Threading.Dispatcher dispatcher, Action onUnauthorized = null, int pageSize = 12)
        {
            _apiClient = apiClient; _loaderService = loaderService; _onUnauthorized = onUnauthorized;
            _pageSize = Math.Max(1, Math.Min(100, pageSize));
            Families = new ObservableCollection<FamilyItem>();
            Categories = new ObservableCollection<Category>();
            Sections = new ObservableCollection<Section>();
            Manufacturers = new ObservableCollection<Manufacturer>();
            RevitVersions = new ObservableCollection<RevitVersion>();
            SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => !_disposed && !_isFamilyLoading);
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters(), _ => !_disposed && !_isFamilyLoading);
            RefreshCommand = new RelayCommand(async _ => await InitializeAsync(), _ => !_disposed && !IsLoading);
            NextPageCommand = new RelayCommand(async _ => { CurrentPage++; await LoadPageAsync(); }, _ => !_disposed && !IsLoading && CurrentPage < TotalPages);
            PreviousPageCommand = new RelayCommand(async _ => { CurrentPage--; await LoadPageAsync(); }, _ => !_disposed && !IsLoading && CurrentPage > 1);
            LoadFamilyCommand = new RelayCommand(async item => await LoadFamilyAsync(item as FamilyItem), item => !_disposed && !IsLoading && item is FamilyItem);
            OpenFamilyDetailsCommand = new RelayCommand(async item => await OpenDetailsAsync(item as FamilyItem), item => !_disposed && !IsLoading && item is FamilyItem);
            LogoutCommand = new RelayCommand(async _ => await LogoutAsync(), _ => !_disposed && !_isFamilyLoading);
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                }
            }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged(nameof(SelectedCategory));
                    QueueSearch();
                }
            }
        }

        public Section SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != value)
                {
                    _selectedSection = value;
                    OnPropertyChanged(nameof(SelectedSection));
                    QueueSearch();
                }
            }
        }

        public Manufacturer SelectedManufacturer
        {
            get => _selectedManufacturer;
            set
            {
                if (_selectedManufacturer != value)
                {
                    _selectedManufacturer = value;
                    OnPropertyChanged(nameof(SelectedManufacturer));
                    QueueSearch();
                }
            }
        }

        public RevitVersion SelectedRevitVersion
        {
            get => _selectedRevitVersion;
            set
            {
                if (_selectedRevitVersion != value)
                {
                    _selectedRevitVersion = value;
                    OnPropertyChanged(nameof(SelectedRevitVersion));
                    QueueSearch();
                }
            }
        }

        public ObservableCollection<FamilyItem> Families
        {
            get => _families;
            set
            {
                _families = value;
                OnPropertyChanged(nameof(Families));
            }
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged(nameof(Categories));
            }
        }

        public ObservableCollection<Section> Sections
        {
            get => _sections;
            set
            {
                _sections = value;
                OnPropertyChanged(nameof(Sections));
            }
        }

        public ObservableCollection<Manufacturer> Manufacturers
        {
            get => _manufacturers;
            set
            {
                _manufacturers = value;
                OnPropertyChanged(nameof(Manufacturers));
            }
        }

        public ObservableCollection<RevitVersion> RevitVersions
        {
            get => _revitVersions;
            set
            {
                _revitVersions = value;
                OnPropertyChanged(nameof(RevitVersions));
            }
        }

        public FamilyItem SelectedFamily
        {
            get => _selectedFamily;
            set
            {
                _selectedFamily = value;
                OnPropertyChanged(nameof(SelectedFamily));
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public string PageInfo => $"Страница {CurrentPage} из {TotalPages} (всего: {TotalCount})";

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        #endregion

        public ICommand SearchCommand { get; }
        public ICommand LoadFamilyCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenFamilyDetailsCommand { get; }
        public ICommand LogoutCommand { get; }

        private void QueueSearch()
        {
            if (_disposed || _suspendSearch) return;
            if (_isFamilyLoading) { _searchPending = true; return; }
            _ = SearchAsync(); // All errors/cancellation are handled by RunRequestAsync.
        }
        private Task SearchAsync() { CurrentPage = 1; return LoadPageAsync(); }
        private Task LoadPageAsync() => RunRequestAsync(FetchPageAsync);
        public Task InitializeAsync() => RunRequestAsync(async ct =>
        {
            var categories = _apiClient.GetCategoriesAsync(ct);
            var sections = _apiClient.GetSectionsAsync(ct);
            var manufacturers = _apiClient.GetManufacturersAsync(ct);
            var versions = _apiClient.GetRevitVersionsAsync(ct);
            await Task.WhenAll(categories, sections, manufacturers, versions);
            ct.ThrowIfCancellationRequested();
            _suspendSearch = true;
            try
            {
                Categories = new ObservableCollection<Category>(await categories);
                Categories.Insert(0, new Category { Id = Guid.Empty, Name = "Все категории" });
                Sections = new ObservableCollection<Section>(await sections);
                Sections.Insert(0, new Section { Id = Guid.Empty, Name = "Все подкатегории" });
                Manufacturers = new ObservableCollection<Manufacturer>(await manufacturers);
                Manufacturers.Insert(0, new Manufacturer { Id = Guid.Empty, Name = "Все производители" });
                RevitVersions = new ObservableCollection<RevitVersion>(await versions);
                RevitVersions.Insert(0, new RevitVersion { Id = Guid.Empty, Name = "Все версии" });
                SelectedCategory = Categories.First();
                SelectedSection = Sections.First();
                SelectedManufacturer = Manufacturers.First();
                SelectedRevitVersion = RevitVersions.First();
                CurrentPage = 1;
            }
            finally { _suspendSearch = false; }
            await FetchPageAsync(ct);
        });

        private async Task RunRequestAsync(Func<CancellationToken, Task> action)
        {
            if (_disposed || _isFamilyLoading) return;
            _request?.Cancel();
            var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _request = request;
            IsLoading = true;
            StatusMessage = "Загрузка...";
            try { await action(request.Token); }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (UnauthorizedAccessException) { if (!_disposed && ReferenceEquals(_request, request)) _onUnauthorized?.Invoke(); }
            catch (Exception ex)
            {
                if (!_disposed && ReferenceEquals(_request, request)) { StatusMessage = ex.Message; SimpleLogger.Error("Catalog request failed", ex); }
            }
            finally
            {
                if (ReferenceEquals(_request, request))
                {
                    _request = null;
                    if (!_disposed) IsLoading = false;
                }
                request.Dispose();
            }
        }
        private async Task FetchPageAsync(CancellationToken ct)
        {
            var result = await _apiClient.GetFamiliesAsync(new FilterOptions
            {
                Search = SearchText,
                CategoryId = SelectedCategory?.Id != Guid.Empty ? SelectedCategory?.Id : null,
                SectionId = SelectedSection?.Id != Guid.Empty ? SelectedSection?.Id : null,
                ManufacturerId = SelectedManufacturer?.Id != Guid.Empty ? SelectedManufacturer?.Id : null,
                RevitVersionId = SelectedRevitVersion?.Id != Guid.Empty ? SelectedRevitVersion?.Id : null,
                Page = CurrentPage, PageSize = _pageSize
            }, ct);
            ct.ThrowIfCancellationRequested();
            // Continuations stay on the WPF dispatcher. A superseded request cannot publish its page.
            Families = new ObservableCollection<FamilyItem>(result.Items);
            TotalPages = Math.Max(1, result.TotalPages);
            TotalCount = result.TotalCount;
            StatusMessage = TotalCount == 0 ? "Ничего не найдено" : "Найдено: " + TotalCount;
        }
        private void ClearFilters()
        {
            _suspendSearch = true;
            try
            {
                SearchText = "";
                SelectedCategory = Categories.FirstOrDefault();
                SelectedSection = Sections.FirstOrDefault();
                SelectedManufacturer = Manufacturers.FirstOrDefault();
                SelectedRevitVersion = RevitVersions.FirstOrDefault();
            }
            finally { _suspendSearch = false; }
            QueueSearch();
        }
        private async Task OpenDetailsAsync(FamilyItem family)
        {
            if (family == null || _disposed) return;
            try
            {
                var detail = await _apiClient.GetFamilyByIdAsync(family.Id, _lifetime.Token);
                if (_disposed) return;
                var window = new FamilyDetailsWindow();
                var vm = new FamilyDetailsViewModel(detail, _apiClient, () => window.Close(), async () =>
                { window.Close(); await LoadFamilyAsync(detail); });
                window.DataContext = vm;
                _details.Add(window);
                try { window.ShowDialog(); }
                finally { _details.Remove(window); vm.Dispose(); }
            }
            catch (OperationCanceledException) when (_disposed) { }
            catch (UnauthorizedAccessException) { if (!_disposed) _onUnauthorized?.Invoke(); }
            catch (Exception ex) { if (!_disposed) StatusMessage = ex.Message; }
        }
        private async Task LoadFamilyAsync(FamilyItem family)
        {
            if (family == null || _disposed || _isFamilyLoading) return;
            _isFamilyLoading = true;
            IsLoading = true;
            try
            {
                await _loaderService.LoadFamilyAsync(family, text => { if (!_disposed) StatusMessage = text; },
                    (ok, message) => { if (!_disposed) StatusMessage = message; }, ct: _lifetime.Token);
            }
            catch (OperationCanceledException) when (_disposed) { }
            catch (UnauthorizedAccessException) { if (!_disposed) _onUnauthorized?.Invoke(); }
            catch (Exception ex) { if (!_disposed) StatusMessage = ex.Message; }
            finally
            {
                _isFamilyLoading = false;
                if (!_disposed)
                {
                    IsLoading = false;
                    if (_searchPending) { _searchPending = false; QueueSearch(); }
                }
            }
        }
        private async Task LogoutAsync()
        {
            if (System.Windows.MessageBox.Show("Выйти из системы?", "BIMHub",
                System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes) return;
            try { await _apiClient.LogoutAsync(_lifetime.Token); }
            catch (Exception ex) { SimpleLogger.Error("Server logout was not confirmed", ex); }
            if (!_disposed) _onUnauthorized?.Invoke();
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
            _request?.Cancel();
            foreach (var window in _details.ToArray()) window.Close();
            _lifetime.Dispose();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
