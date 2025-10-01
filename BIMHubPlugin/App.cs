using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace BIMHubPlugin
{
    public class App : IExternalApplication
    {
        private static DockablePaneId _dockablePaneId = new DockablePaneId(new Guid("A7B3C8D9-1234-5678-90AB-CDEF12345678"));
        private static CatalogPaneProvider _catalogProvider;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                RegisterDockablePane(application);
                CreateRibbonTab(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка запуска плагина", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void RegisterDockablePane(UIControlledApplication application)
        {
            _catalogProvider = new CatalogPaneProvider();
            application.RegisterDockablePane(_dockablePaneId, "BIMHubPlugin", _catalogProvider);
        }

        private void CreateRibbonTab(UIControlledApplication application)
        {
            string tabName = "KAZGOR";
            
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "BIMHub");
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            
            PushButtonData buttonData = new PushButtonData(
                "ShowCatalog",
                "Каталог семейств",
                assemblyPath,
                "BIMHubPlugin.Commands.ShowCatalogCommand"
            );

            Uri iconUri = new Uri("pack://application:,,,/BIMHubPlugin;component/Resources/icon.png");
            buttonData.LargeImage = new BitmapImage(iconUri);
            buttonData.ToolTip = "Открыть каталог семейств BIMHub";

            panel.AddItem(buttonData);
        }

        public static DockablePaneId GetDockablePaneId()
        {
            return _dockablePaneId;
        }

        public static CatalogPaneProvider GetCatalogProvider()
        {
            return _catalogProvider;
        }
    }

    public class CatalogPaneProvider : IDockablePaneProvider
    {
        private Views.CatalogView _catalogView;

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            if (_catalogView == null)
            {
                _catalogView = new Views.CatalogView();
            }

            data.FrameworkElement = _catalogView;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }

        public Views.CatalogView GetCatalogView()
        {
            return _catalogView;
        }
    }
}