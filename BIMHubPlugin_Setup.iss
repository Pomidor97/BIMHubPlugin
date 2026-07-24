; BIMHub Plugin for Revit 2023 - Inno Setup Script

#define MyAppName "BIMHub Plugin для Revit"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "BIMHub"
#define MyAppURL "http://bimhub.kazgor.kz"
#define MyAppExeName "BIMHubPlugin.dll"

[Setup]
; Уникальный ID приложения (не меняйте после релиза!)
AppId={{A7B3C8D9-1234-5678-90AB-CDEF12345678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Куда устанавливать плагин
DefaultDirName={userappdata}\Autodesk\Revit\Addins\2023\BIMHubPlugin
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Где создавать установщик
OutputDir=.\Output
OutputBaseFilename=BIMHubPlugin_Setup_v{#MyAppVersion}_Revit2023

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
VersionInfoDescription=BIMHub Plugin для Autodesk Revit 2023

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Полные пути к файлам (замените на ваш реальный путь)
Source: "D:\Zhassulan\03_Source codes\BIMHubPlugin\BIMHubPlugin\bin\Release\net48\BIMHubPlugin.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Zhassulan\03_Source codes\BIMHubPlugin\BIMHubPlugin\bin\Release\net48\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion

; Ресурсы
Source: "D:\Zhassulan\03_Source codes\BIMHubPlugin\BIMHubPlugin\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs createallsubdirs

; .addin файл
Source: "D:\Zhassulan\03_Source codes\BIMHubPlugin\BIMHubPlugin\BIMHubPlugin.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion

; Документация (опционально)
; Source: "readme.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
; Source: "license.txt"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
; Создаем папку для конфигурации с полными правами
Name: "{localappdata}\BIMHubPlugin"; Permissions: users-full
Name: "{localappdata}\BIMHubPlugin\Cache"; Permissions: users-full
Name: "{localappdata}\BIMHubPlugin\Logs"; Permissions: users-full

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
  ConfigPage.Values[0] := 'http://bimhub.kazgor.kz/api';
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
      '⚠ ВАЖНО: Перезапустите Revit 2023 для загрузки плагина.' + #13#10 + #13#10 +
      'После запуска Revit откройте вкладку "BIM Manager" и нажмите "Открыть Каталог".';
  end;
end;

// Проверка: установлен ли Revit 2023
function InitializeSetup(): Boolean;
var
  RevitAddinsFolder: String;
begin
  Result := True;
  RevitAddinsFolder := ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2023');
  
  // Предупреждение если папка Revit 2023 не найдена
  if not DirExists(RevitAddinsFolder) then
  begin
    if MsgBox('Папка Revit 2023 Addins не найдена.' + #13#10 + 
              'Возможно, Revit 2023 не установлен.' + #13#10 + #13#10 +
              'Продолжить установку?', 
              mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

[UninstallDelete]
; Удаляем кэш при деинсталляции
Type: filesandordirs; Name: "{localappdata}\BIMHubPlugin\Cache"
Type: filesandordirs; Name: "{localappdata}\BIMHubPlugin\Logs"

[Messages]
russian.WelcomeLabel2=Программа установит [name/ver] на ваш компьютер.%n%nПлагин добавит каталог семейств BIMHub в Autodesk Revit 2023.%n%nРекомендуется закрыть Revit перед продолжением установки.
english.WelcomeLabel2=This will install [name/ver] on your computer.%n%nThe plugin will add BIMHub family catalog to Autodesk Revit 2023.%n%nIt is recommended that you close Revit before continuing.