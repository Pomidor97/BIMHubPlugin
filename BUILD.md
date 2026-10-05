# Сборка и проверка плагина

Плагин использует .NET Framework 4.8. Для каждой версии нужен установленный Revit с соответствующими RevitAPI.dll и RevitAPIUI.dll. SDK не включён в репозиторий.

```powershell
dotnet restore BIMHubPlugin.sln
dotnet test tests/BIMHubPlugin.Core.Tests
dotnet build BIMHubPlugin/BIMHubPlugin.csproj -c Release -p:RevitVersion=2023
dotnet build BIMHubPlugin/BIMHubPlugin.csproj -c Release -p:RevitVersion=2024
& 'C:/Program Files (x86)/Inno Setup 6/ISCC.exe' /DRevitVersion=2023 BIMHubPlugin_Setup.iss
& 'C:/Program Files (x86)/Inno Setup 6/ISCC.exe' /DRevitVersion=2024 BIMHubPlugin_Setup.iss
```

Путь SDK переопределяется `-p:RevitInstallDir=...`. Сборка сохраняет DLL в `BIMHubPlugin/bin/Revit<год>/Release/net48`; установка при сборке выключена. Для явной локальной установки можно передать `-p:DeployAddin=true`. Установщики двух версий имеют отдельные идентификаторы и используют общий пользовательский конфиг.

Перед выпуском проверьте доступность HTTPS на сервере. Старый адрес `http://bimhub.kazgor.kz/api` автоматически переводится в HTTPS; удалённые HTTP-серверы отклоняются до передачи пароля и JWT. HTTP разрешён только для loopback-разработки. Не отключайте проверку сертификатов.

Кэш зависит от URL и UpdatedAt семейства, учитывает TTL и размер в байтах. Активные загрузки держат файл до завершения ExternalEvent. Для загрузки в документ поддерживается RFA; RVT/RTE требуют открытия документа. Перед использованием кэша клиент проверяет текущий доступ и метаданные через API.

Проверка непосредственно в Revit 2023 и 2024 перед выпуском: вход/выход, быстрые переключения фильтров, поиск и пагинация, просмотр карточки, повторная загрузка обновлённого RFA, смена/закрытие документа во время скачивания, отказ при загрузке более новой версии семейства, завершение Revit во время запроса. Автоматические тесты ядра и компиляция не заменяют эти проверки Revit API.
