; BIMHub Plugin for Revit 2023 - Inno Setup Script

#ifndef RevitVersion
  #define RevitVersion "2023"
#endif
#if (RevitVersion != "2023") && (RevitVersion != "2024")
  #error Unsupported RevitVersion
#endif

#define MyAppName "BIMHub Plugin для Revit"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "BIMHub"
#define MyAppURL "https://bimhub.kazgor.kz"
#define MyAppExeName "BIMHubPlugin.dll"

[Setup]
; Уникальный ID приложения (не меняйте после релиза!)
#if RevitVersion == "2024"
AppId={{A7B3C8D9-1234-5678-90AB-CDEF12345679}
#else
AppId={{A7B3C8D9-1234-5678-90AB-CDEF12345678}
#endif
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Куда устанавливать плагин
DefaultDirName={userappdata}\Autodesk\Revit\Addins\{#RevitVersion}\BIMHubPlugin
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Где создавать установщик
OutputDir=.\Output
OutputBaseFilename=BIMHubPlugin_Setup_v{#MyAppVersion}_Revit{#RevitVersion}

; Настройки сжатия
Compression=lzma2
SolidCompression=yes

; Внешний вид
WizardStyle=modern
WizardSizePercent=120

; Права доступа (не требует админа)
PrivilegesRequired=lowest

; Иконка деинсталлятора
UninstallDisplayIcon={app}\Resources\icon.png

; Информация о версии
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=BIMHub Plugin для Autodesk Revit {#RevitVersion}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Полные пути к файлам (замените на ваш реальный путь)
Source: "BIMHubPlugin\bin\Revit{#RevitVersion}\Release\net48\BIMHubPlugin.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "BIMHubPlugin\bin\Revit{#RevitVersion}\Release\net48\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion

; Ресурсы
Source: "BIMHubPlugin\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs createallsubdirs

; .addin файл
Source: "BIMHubPlugin\BIMHubPlugin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\{#RevitVersion}"; Flags: ignoreversion

; Документация (опционально)
; Source: "readme.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
; Source: "license.txt"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
; Создаем папку для конфигурации с полными правами
Name: "{localappdata}\BIMHubPlugin"
Name: "{localappdata}\BIMHubPlugin\Cache"
Name: "{localappdata}\BIMHubPlugin\Logs"

[Icons]
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Run]
; Опционально: открыть папку с конфигом после установки
Filename: "notepad.exe"; Parameters: "{localappdata}\BIMHubPlugin\config.json"; Description: "Открыть файл конфигурации"; Flags: postinstall shellexec skipifsilent nowait

[Code]
var
  ConfigPage: TInputQueryWizardPage;
  ApiUrlEdit: String;

// Создаем страницу настройки API
procedure InitializeWizard;
begin
  ConfigPage := CreateInputQueryPage(wpSelectDir,
    'Настройка подключения к API', 
    'Укажите адрес сервера BIMHub',
    'Введите URL API сервера. Вы сможете изменить это позже в файле config.json');
  
  ConfigPage.Add('API URL:', False);
  ConfigPage.Values[0] := 'https://bimhub.kazgor.kz/api';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Url: String;
begin
  Result := True;
  if CurPageID = ConfigPage.ID then
  begin
    Url := Trim(ConfigPage.Values[0]);
    Result := (Pos('https://', Lowercase(Url)) = 1) and (Pos('"', Url) = 0) and (Pos('\', Url) = 0);
    if not Result then
      MsgBox('Укажите HTTPS-адрес API без кавычек и обратных слешей.', mbError, MB_OK);
  end;
end;

// Создаем config.json при установке
procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFolder: String;
  ConfigFile: String;
  ApiUrl: String;
begin
  if CurStep = ssPostInstall then
  begin
    ConfigFolder := ExpandConstant('{localappdata}\BIMHubPlugin');
    ConfigFile := ConfigFolder + '\config.json';
    ApiUrl := ConfigPage.Values[0];
    
    // Создаем папки если не существуют
    if not DirExists(ConfigFolder) then
      ForceDirectories(ConfigFolder);
    
    if not DirExists(ConfigFolder + '\Cache') then
      ForceDirectories(ConfigFolder + '\Cache');
      
    if not DirExists(ConfigFolder + '\Logs') then
      ForceDirectories(ConfigFolder + '\Logs');
    
    // Создаем config.json только если его нет
    if not FileExists(ConfigFile) then
    begin
      SaveStringToFile(ConfigFile, '{' + #13#10, False);
      SaveStringToFile(ConfigFile, '  "ApiBaseUrl": "' + ApiUrl + '",' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "CacheSizeMB": 500,' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "RequestTimeoutSeconds": 300,' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "DefaultPageSize": 12,' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "EnableLogging": true,' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "CacheFolder": "",' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "LogFolder": "",' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "Language": "ru",' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "CheckForUpdates": true,' + #13#10, True);
      SaveStringToFile(ConfigFile, '  "CacheTTLDays": 7' + #13#10, True);
      SaveStringToFile(ConfigFile, '}' + #13#10, True);
    end;
  end;
end;

// Показываем финальное сообщение
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    WizardForm.FinishedLabel.Caption := 
      'Установка BIMHub Plugin завершена!' + #13#10 + #13#10 +
      'Плагин установлен в:' + #13#10 +
      ExpandConstant('{app}') + #13#10 + #13#10 +
      'Файл конфигурации:' + #13#10 +
      ExpandConstant('{localappdata}\BIMHubPlugin\config.json') + #13#10 + #13#10 +
      '⚠ ВАЖНО: Перезапустите Revit {#RevitVersion} для загрузки плагина.' + #13#10 + #13#10 +
      'После запуска Revit откройте вкладку "KAZGOR" и нажмите "Каталог семейств".';
  end;
end;

// Проверка: установлен ли Revit {#RevitVersion}
function InitializeSetup(): Boolean;
var
  RevitAddinsFolder: String;
begin
  Result := True;
  RevitAddinsFolder := ExpandConstant('{userappdata}\Autodesk\Revit\Addins\{#RevitVersion}');
  
  // Предупреждение если папка Revit {#RevitVersion} не найдена
  if not DirExists(RevitAddinsFolder) then
  begin
    if MsgBox('Папка Revit {#RevitVersion} Addins не найдена.' + #13#10 + 
              'Возможно, Revit {#RevitVersion} не установлен.' + #13#10 + #13#10 +
              'Продолжить установку?', 
              mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

// Shared cache/configuration remain available to the other Revit version.

[Messages]
russian.WelcomeLabel2=Программа установит [name/ver] на ваш компьютер.%n%nПлагин добавит каталог семейств BIMHub в Autodesk Revit {#RevitVersion}.%n%nРекомендуется закрыть Revit перед продолжением установки.
english.WelcomeLabel2=This will install [name/ver] on your computer.%n%nThe plugin will add BIMHub family catalog to Autodesk Revit {#RevitVersion}.%n%nIt is recommended that you close Revit before continuing.
