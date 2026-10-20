#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
; AppId должен оставаться неизменным между версиями (по нему Windows понимает, что это обновление)
AppId={{B3F1C2A4-6D7E-4F38-9A51-2C8E5D0F7A11}
AppName=HoverDrop
AppVersion={#AppVersion}
AppPublisher=TimofeyDanilyuk
DefaultDirName={autopf}\HoverDrop
DefaultGroupName=HoverDrop
OutputDir=..\dist
OutputBaseFilename=HoverDrop-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
; Установка для текущего пользователя, без запроса прав администратора
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Не даёт ставить поверх запущенного приложения (мьютекс из App.xaml.cs)
AppMutex=HoverDrop.SingleInstance
WizardStyle=modern
UninstallDisplayIcon={app}\HoverDrop.App.exe

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\HoverDrop"; Filename: "{app}\HoverDrop.App.exe"
Name: "{group}\Удалить HoverDrop"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\HoverDrop.App.exe"; Description: "Запустить HoverDrop"; Flags: nowait postinstall skipifsilent