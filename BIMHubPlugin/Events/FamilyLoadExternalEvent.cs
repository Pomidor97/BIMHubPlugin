using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMHubPlugin.Events
{
    /// <summary>
    /// IExternalEvent для загрузки семейства в Revit в главном потоке
    /// </summary>
    public class FamilyLoadExternalEvent : IExternalEventHandler
    {
        private string _familyFilePath;
        private Action<bool, string> _callback;

        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument?.Document;

            if (doc == null)
            {
                _callback?.Invoke(false, "Нет активного документа");
                return;
            }

            if (string.IsNullOrEmpty(_familyFilePath) || !File.Exists(_familyFilePath))
            {
                _callback?.Invoke(false, $"Файл не найден: {_familyFilePath}");
                return;
            }

            try
            {
                bool success = false;
                string message = "";

                // Начинаем транзакцию
                using (Transaction trans = new Transaction(doc, "Загрузка семейства"))
                {
                    trans.Start();

                    try
                    {
                        // Загружаем семейство
                        Family family;
                        bool loaded = doc.LoadFamily(_familyFilePath, out family);

                        if (loaded && family != null)
                        {
                            success = true;
                            message = $"Семейство '{family.Name}' успешно загружено";
                        }
                        else
                        {
                            success = false;
                            message = "Не удалось загрузить семейство (возможно, уже существует)";
                        }

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        success = false;
                        message = $"Ошибка загрузки: {ex.Message}";
                    }
                }

                _callback?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                _callback?.Invoke(false, $"Ошибка: {ex.Message}");
            }
            finally
            {
                // Очищаем после выполнения
                _familyFilePath = null;
                _callback = null;
            }
        }

        public string GetName()
        {
            return "FamilyLoadExternalEvent";
        }

        /// <summary>
        /// Установить данные для загрузки
        /// </summary>
        public void SetLoadData(string familyFilePath, Action<bool, string> callback)
        {
            _familyFilePath = familyFilePath;
            _callback = callback;
        }
    }
}