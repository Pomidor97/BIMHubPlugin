using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMHubPlugin.Commands
{   
    
    [Transaction(TransactionMode.ReadOnly)]
    public class ShowCatalogCommand: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                DockablePaneId paneId = App.GetDockablePaneId();
                
                DockablePane pane = uiapp.GetDockablePane(paneId);

                if (pane != null)
                {
                    if (pane.IsShown())
                    {
                        pane.Hide();
                    }
                    else
                    {
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