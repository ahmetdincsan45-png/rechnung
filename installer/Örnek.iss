#define MyAppName "Rechnung"
#define MyAppPublisher "Rechnung"
#define MyAppURL "https://rechnung.local"
#define MyAppExeName "Rechnung.exe"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef MyAppPublishDir
  #define MyAppPublishDir ".\publish"
#endif

#ifndef MyAppSetupOutDir
  #define MyAppSetupOutDir "."
#endif

[Setup]
AppId={{7E4D6E2F-2D91-4B60-A5A0-8E9A08C5B8B1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
OutputDir={#MyAppSetupOutDir}
OutputBaseFilename=Rechnung-Setup-{#MyAppVersion}
SetupIconFile=..\Örnek\Assets\ADN_Objektservice_1px_margin.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Rechnung Rechnungsprogramm Setup
VersionInfoProductName={#MyAppName}
WizardImageStretch=no
UninstallDisplayName={#MyAppName}

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
	MsgBox('Benutzerdaten und Archivinhalte wurden nicht gelöscht.', mbInformation, MB_OK);
  end;
end;
