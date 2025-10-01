using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using BIMHubPlugin.Views;

namespace BIMHubPlugin
{
    public class App: IExternalApplication
    {
        private static CatalogView _catalogView;
        private static DockablePaneId _dockablePaneId = 
            new DockablePaneId(new Guid("A7B3C8D9-1234-5678-90AB-CDEF12345678"));
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                RegisterDocalblePane(application);
                
                CreatRibbonPanel(application);
                
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void RegisterDocalblePane(UIControlledApplication application)
        {
            _catalogView = new CatalogView();
            application.RegisterDockablePane(
                _dockablePaneId,
                "BIMHub Plugin",
                _catalogView);
        }

        private void CreatRibbonPanel(UIControlledApplication application)
        {
            string tabName = "KAZGOR";

            try { application.CreateRibbonTab(tabName); }
            catch {}
            
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Catalog");
            
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData(
                "ShowCatalog",
                "Открыть\nКаталог",
                assemblyPath,
                "BimManagerRevitPlugin.Commands.ShowCatalogCommand"
            );

            Uri iconUri = new Uri("BIMHubPlugin/Resources/KAZGOR FM_LOGO_32x32.png");
            buttonData.LargeImage = new BitmapImage(iconUri);
            
            buttonData.ToolTip = "ShowCatalog";
            
            panel.AddItem(buttonData);
        }

        private static DockablePaneId GetDockablePaneId()
        {
            return _dockablePaneId;
        }
    }
}
