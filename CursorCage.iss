; =============================================================================
; CursorCage — script Inno Setup 6
; https://jrsoftware.org/isinfo.php
;
; Prérequis : dossier artifacts\publish rempli par :
;   .\scripts\Build-Installer.ps1
; ou manuellement :
;   dotnet publish CursorCage.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish
;
; Compilation (ligne de commande) :
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" CursorCage.iss
;
; Sortie : artifacts\installer\CursorCage-Setup.exe
; (nom attendu par la mise à jour GitHub intégrée à l’app)
; =============================================================================

#define MyAppName "CursorCage"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "erwang64"
#define MyAppExeName "CursorCage.exe"
#define MyAppURL "https://github.com/erwang64/CursorCage"

[Setup]
AppId={{E8F4A1B2-3C5D-4E6F-8091-A2B3C4D5E6F7}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=artifacts\installer
OutputBaseFilename=CursorCage-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=Logo\Logo_blanc.ico
DisableProgramGroupPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
