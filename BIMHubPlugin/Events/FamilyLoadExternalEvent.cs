using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMHubPlugin.Services;
using System;
using System.IO;

namespace BIMHubPlugin.Events
{
    /// <summary>
    /// Обработчик внешнего события для загрузки семейств в Revit
    /// </summary>
    public class FamilyLoadExternalEvent : IExternalEventHandler
    {
        private string _familyFilePath;
        private string _nameRfa;
        private bool _showDialog;
        private Action<bool, string> _callback;

        /// <summary>
        /// Выполняет загрузку семейства в документ Revit
        /// </summary>
        public void Execute(UIApplication app)
        {
            SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Начало выполнения");
            
            Document doc = app.ActiveUIDocument?.Document;

            if (doc == null)
            {
                SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Нет активного документа");
                _callback?.Invoke(false, "Нет активного документа");
                return;
            }

            if (string.IsNullOrEmpty(_familyFilePath) || !File.Exists(_familyFilePath))
            {
                SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Файл не найден: {_familyFilePath}");
                _callback?.Invoke(false, $"Файл не найден: {_familyFilePath}");
                return;
            }

            var fileInfo = new FileInfo(_familyFilePath);
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Загрузка файла: {_familyFilePath}");
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Имя из БД (NameRfa): {_nameRfa}");
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Показывать диалог: {_showDialog}");
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Файл существует: {fileInfo.Exists}");
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Размер файла: {fileInfo.Length} байт");
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Расширение: {fileInfo.Extension}");
            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Версия Revit документа: {doc.Application.VersionNumber}");
            
            try
            {
                bool success = false;
                string message = "";

                using (Transaction trans = new Transaction(doc, "Загрузка семейства"))
                {
                    trans.Start();
                    SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Транзакция начата");

                    try
                    {
                        Family family;
                        bool loaded;

                        if (_showDialog)
                        {
                            // Загрузка с возможностью показа диалога (стандартное поведение Revit)
                            SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Загрузка с опцией диалога");
                            FamilyLoadOptions loadOptions = new FamilyLoadOptions(true);
                            loaded = doc.LoadFamily(_familyFilePath, loadOptions, out family);
                        }
                        else
                        {
                            // Автоматическая замена без диалога
                            SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Автоматическая загрузка без диалога");
                            FamilyLoadOptions loadOptions = new FamilyLoadOptions(false);
                            loaded = doc.LoadFamily(_familyFilePath, loadOptions, out family);
                        }

                        SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: LoadFamily вернул {loaded}, family == null: {family == null}");

                        if (family != null)
                        {
                            success = true;
                            string displayName = !string.IsNullOrEmpty(_nameRfa) ? _nameRfa : family.Name;
        
                            if (loaded)
                            {
                                message = $"Семейство '{displayName}' успешно загружено";
                                SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Новое семейство загружено");
                            }
                            else
                            {
                                message = $"Семейство '{displayName}' обновлено";
                                SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Существующее семейство обновлено/заменено");
                            }
                        }
                        else if (!loaded)
                        {
                            // Пользователь отменил загрузку в диалоге или семейство идентично существующему
                            success = false;
                            message = "Загрузка отменена (внутренняя ошибка или семейство без изменений)";
                            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Загрузка отменена или семейство идентично");
                        }
                        else
                        {
                            success = false;
                            message = "Не удалось загрузить семейство";
                            SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: ОШИБКА - неизвестная причина");
                        }

                        SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Фиксация транзакции...");
                        trans.Commit();
                        SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Транзакция зафиксирована");
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Error("FamilyLoadExternalEvent.Execute: Исключение при LoadFamily", ex);
                        trans.RollBack();
                        success = false;
                        message = $"Ошибка загрузки: {ex.Message}";
                    }
                }

                SimpleLogger.Log($"FamilyLoadExternalEvent.Execute: Вызов callback - Успех: {success}");
                _callback?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("FamilyLoadExternalEvent.Execute: Внешнее исключение", ex);
                _callback?.Invoke(false, $"Ошибка: {ex.Message}");
            }
            finally
            {
                // Очистка данных после выполнения
                _familyFilePath = null;
                _nameRfa = null;
                _showDialog = true;
                _callback = null;
                SimpleLogger.Log("FamilyLoadExternalEvent.Execute: Завершено");
            }
        }

        /// <summary>
        /// Возвращает имя обработчика события
        /// </summary>
        public string GetName()
        {
            return "FamilyLoadExternalEvent";
        }

        /// <summary>
        /// Устанавливает данные для загрузки семейства
        /// </summary>
        /// <param name="familyFilePath">Путь к файлу семейства (.rfa)</param>
        /// <param name="nameRfa">Имя семейства из базы данных</param>
        /// <param name="callback">Callback-функция для уведомления о результате (успех, сообщение)</param>
        /// <param name="showDialog">Показывать ли диалог Revit при замене существующего семейства (по умолчанию true)</param>
        public void SetLoadData(string familyFilePath, string nameRfa, Action<bool, string> callback, bool showDialog = true)
        {
            _familyFilePath = familyFilePath;
            _nameRfa = nameRfa;
            _callback = callback;
            _showDialog = showDialog;
        }
    }
}