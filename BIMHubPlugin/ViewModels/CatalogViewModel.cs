using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BIMHubPlugin.Models;
using BIMHubPlugin.Services;

namespace BIMHubPlugin.ViewModels
{
    /// <summary>
    /// ViewModel для каталога (MVVM pattern)
    /// </summary>
    public class CatalogViewModel : INotifyPropertyChanged
    {
        private readonly CatalogApiClient _apiClient;
        private readonly FamilyLoaderService _loaderService;
        private readonly CacheService _cacheService;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        // Фильтры
        private string _searchText;
        private Category _selectedCategory;
        private Section _selectedSection;
        private Manufacturer _selectedManufacturer;
        private RevitVersion _selectedRevitVersion;

        // Данные
        private ObservableCollection<FamilyItem> _families;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Section> _sections;
        private ObservableCollection<Manufacturer> _manufacturers;
        private ObservableCollection<RevitVersion> _revitVersions;

        // Пагинация
        private int _currentPage = 1;
        private int _pageSize = 12;
        private int _totalPages;
        private int _totalCount;

        // UI состояние
        private bool _isLoading;
        private string _statusMessage;
        private FamilyItem _selectedFamily;

        public CatalogViewModel(
            CatalogApiClient apiClient, 
            FamilyLoaderService loaderService, 
            CacheService cacheService,
            System.Windows.Threading.Dispatcher dispatcher)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _loaderService = loaderService ?? throw new ArgumentNullException(nameof(loaderService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            // Инициализация коллекций
            Families = new ObservableCollection<FamilyItem>();
            Categories = new ObservableCollection<Category>();
            Sections = new ObservableCollection<Section>();
            Manufacturers = new ObservableCollection<Manufacturer>();
            RevitVersions = new ObservableCollection<RevitVersion>();

            // Команды
            SearchCommand = new RelayCommand(async _ => await SearchAsync());
            LoadFamilyCommand = new RelayCommand(async param => await LoadFamilyAsync(param as FamilyItem), 
                param => param is FamilyItem && !IsLoading);
            NextPageCommand = new RelayCommand(async _ => await NextPageAsync(), _ => CurrentPage < TotalPages && !IsLoading);
            PreviousPageCommand = new RelayCommand(async _ => await PreviousPageAsync(), _ => CurrentPage > 1 && !IsLoading);
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            RefreshCommand = new RelayCommand(async _ => await LoadInitialDataAsync());

            // Загружаем начальные данные
            Task.Run(async () => await LoadInitialDataAsync());
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
                    Task.Run(async () => await SearchAsync());
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
                    Task.Run(async () => await SearchAsync());
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
                    Task.Run(async () => await SearchAsync());
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
                    Task.Run(async () => await SearchAsync());
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

        
        
        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand LoadFamilyCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Загрузка начальных данных (фильтры + первая страница семейств)
        /// </summary>
        private async Task LoadInitialDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Загрузка данных...";

            SimpleLogger.Log("LoadInitialDataAsync started");

            var categoriesTask = _apiClient.GetCategoriesAsync();
            var sectionsTask = _apiClient.GetSectionsAsync();
            var manufacturersTask = _apiClient.GetManufacturersAsync();
            var versionsTask = _apiClient.GetRevitVersionsAsync();

            await Task.WhenAll(categoriesTask, sectionsTask, manufacturersTask, versionsTask);

            SimpleLogger.Log($"Loaded {categoriesTask.Result.Count} categories");

            // Используем Dispatcher для обновления UI коллекций
            _dispatcher.Invoke(() =>
            {
                SimpleLogger.Log("Updating UI collections...");
                
                Categories.Clear();
                Categories.Add(new Category { Id = Guid.Empty, Name = "Все категории" });
                foreach (var cat in categoriesTask.Result)
                    Categories.Add(cat);

                Sections.Clear();
                Sections.Add(new Section { Id = Guid.Empty, Name = "Все разделы" });
                foreach (var sec in sectionsTask.Result)
                    Sections.Add(sec);

                Manufacturers.Clear();
                Manufacturers.Add(new Manufacturer { Id = Guid.Empty, Name = "Все производители" });
                foreach (var man in manufacturersTask.Result)
                    Manufacturers.Add(man);

                RevitVersions.Clear();
                RevitVersions.Add(new RevitVersion { Id = Guid.Empty, Name = "Все версии" });
                foreach (var ver in versionsTask.Result)
                    RevitVersions.Add(ver);

                SelectedCategory = Categories.FirstOrDefault();
                SelectedSection = Sections.FirstOrDefault();
                SelectedManufacturer = Manufacturers.FirstOrDefault();
                SelectedRevitVersion = RevitVersions.FirstOrDefault();
            });

            SimpleLogger.Log("UI update completed");

            await LoadPageAsync();

            StatusMessage = "Готово";
            SimpleLogger.Log("LoadInitialDataAsync completed successfully");
        }
        catch (Exception ex)
        {
            SimpleLogger.Error("LoadInitialDataAsync failed", ex);
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

        /// <summary>
        /// Поиск семейств с текущими фильтрами
        /// </summary>
        private async Task SearchAsync()
        {
            try
            {
                CurrentPage = 1; // Сбрасываем на первую страницу
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка поиска: {ex.Message}";
            }
        }

        /// <summary>
        /// Загрузить следующую страницу
        /// </summary>
        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages) return;

            CurrentPage++;
            await LoadPageAsync();
        }

        /// <summary>
        /// Загрузить предыдущую страницу
        /// </summary>
        private async Task PreviousPageAsync()
        {
            if (CurrentPage <= 1) return;

            CurrentPage--;
            await LoadPageAsync();
        }

        /// <summary>
        /// Загрузить текущую страницу с фильтрами
        /// </summary>
        private async Task LoadPageAsync()
        {
            try
            {
                SimpleLogger.Log($"LoadPageAsync started - Page {CurrentPage}");
                IsLoading = true;

                var filter = new FilterOptions
                {
                    Search = SearchText,
                    CategoryId = SelectedCategory?.Id != Guid.Empty ? SelectedCategory?.Id : null,
                    SectionId = SelectedSection?.Id != Guid.Empty ? SelectedSection?.Id : null,
                    ManufacturerId = SelectedManufacturer?.Id != Guid.Empty ? SelectedManufacturer?.Id : null,
                    RevitVersionId = SelectedRevitVersion?.Id != Guid.Empty ? SelectedRevitVersion?.Id : null,
                    Page = CurrentPage,
                    PageSize = _pageSize
                };

                var result = await _apiClient.GetFamiliesAsync(filter);

                SimpleLogger.Log($"Received {result.Items.Count} families");

                // Обновляем через Dispatcher
                _dispatcher.Invoke(() =>
                {
                    Families.Clear();
                    foreach (var family in result.Items)
                        Families.Add(family);

                    TotalPages = result.TotalPages;
                    TotalCount = result.TotalCount;
                });

                StatusMessage = result.TotalCount > 0 
                    ? $"Найдено: {result.TotalCount}" 
                    : "Ничего не найдено";
                
                SimpleLogger.Log($"LoadPageAsync completed - Total: {result.TotalCount}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("LoadPageAsync failed", ex);
                StatusMessage = $"Ошибка: {ex.Message}";
            
                _dispatcher.Invoke(() =>
                {
                    Families.Clear();
                    TotalPages = 0;
                    TotalCount = 0;
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Загрузить семейство в Revit
        /// </summary>
        private async Task LoadFamilyAsync(FamilyItem family)
        {
            if (family == null) return;

            try
            {
                IsLoading = true;
                SelectedFamily = family;

                await _loaderService.LoadFamilyAsync(
                    family,
                    progressMessage => 
                    {
                        // Обновляем статус в UI потоке
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = progressMessage;
                        });
                    },
                    (success, message) =>
                    {
                        // Callback выполняется в главном потоке Revit
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = message;
                            IsLoading = false;

                            if (success)
                            {
                                System.Windows.MessageBox.Show(
                                    message,
                                    "Успех",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Information
                                );
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    message,
                                    "Ошибка",
                                    System.Windows.MessageBoxButton.OK,
                                    System.Windows.MessageBoxImage.Error
                                );
                            }
                        });
                    }
                );
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                    IsLoading = false;

                    System.Windows.MessageBox.Show(
                        $"Ошибка загрузки семейства:\n{ex.Message}",
                        "Ошибка",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error
                    );
                });
            }
        }

        /// <summary>
        /// Очистить все фильтры
        /// </summary>
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = Categories.FirstOrDefault();
            SelectedSection = Sections.FirstOrDefault();
            SelectedManufacturer = Manufacturers.FirstOrDefault();
            SelectedRevitVersion = RevitVersions.FirstOrDefault();
            
            // Автоматически выполняем поиск после очистки
            Task.Run(async () => await SearchAsync());
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
        
        
    }
}