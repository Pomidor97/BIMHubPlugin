using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMHubPlugin.Events
{
    /// <summary>
    /// Опции загрузки семейства с поддержкой диалогов Revit
    /// </summary>
    internal class FamilyLoadOptions : IFamilyLoadOptions
    {
        private readonly bool _allowUserInteraction;

        public FamilyLoadOptions(bool allowUserInteraction = true)
        {
            _allowUserInteraction = allowUserInteraction;
        }

        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            if (_allowUserInteraction)
            {
                // Показываем стандартный диалог Revit
                TaskDialog dialog = new TaskDialog("Семейство уже существует");
                dialog.MainInstruction = "Попытка загрузки семейства, уже имеющегося в данном проекте.";
                dialog.MainContent = "Выберите одну из следующих возможностей:";
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Заменить существующую версию");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Заменить существующую версию и значения параметров");
                dialog.CommonButtons = TaskDialogCommonButtons.Cancel;
                dialog.DefaultButton = TaskDialogResult.Cancel;

                TaskDialogResult result = dialog.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    overwriteParameterValues = false;
                    return true;
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    overwriteParameterValues = true;
                    return true;
                }
                else
                {
                    // Пользователь отменил
                    overwriteParameterValues = false;
                    return false;
                }
            }
            else
            {
                // Автоматическая замена
                overwriteParameterValues = true;
                return true;
            }
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}