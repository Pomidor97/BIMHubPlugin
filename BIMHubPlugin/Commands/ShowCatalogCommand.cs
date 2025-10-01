using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMHubPlugin.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ShowCatalogCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                DockablePaneId paneId = App.GetDockablePaneId();
                DockablePane pane = uiApp.GetDockablePane(paneId);

                if (pane != null)
                {
                    if (pane.IsShown())
                    {
                        pane.Hide();
                    }
                    else
                    {
                        // Получаем CatalogView через Provider
                        var provider = App.GetCatalogProvider();
                        if (provider != null)
                        {
                            var catalogView = provider.GetCatalogView();
                            if (catalogView != null)
                            {
                                catalogView.SetUIApplication(uiApp);
                            }
                        }
                        
                        pane.Show();
                    }
                }

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}