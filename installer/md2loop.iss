; md2loop Inno Setup installer script
; Builds a modern Windows installer with Start Menu, Desktop shortcut, and uninstaller

#define MyAppName "md2loop"
#ifndef MyAppVersion
#define MyAppVersion "0.0.0"
#endif

#ifdef MyAppArm64
#define MyAppRid "win-arm64"
#define MyAppArchitecturesAllowed "arm64"
#define MyAppArchitecturesInstallIn64BitMode "arm64"
#else
#define MyAppRid "win-x64"
#define MyAppArchitecturesAllowed "x64compatible and not arm64"
#define MyAppArchitecturesInstallIn64BitMode "x64compatible"
#endif

#define MyAppPublisher "trsdn"
#define MyAppURL "https://github.com/trsdn/md2loop-windows"
#define MyAppExeName "md2loop.exe"

[Setup]
AppId={{B8A3D2E1-5F7C-4A9B-8D6E-1C2F3A4B5D6E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=md2loop-setup-{#MyAppVersion}-{#MyAppRid}
SetupIconFile=..\assets\logo.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed={#MyAppArchitecturesAllowed}
ArchitecturesInstallIn64BitMode={#MyAppArchitecturesInstallIn64BitMode}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupentry"; Description: "Start md2loop with Windows"; GroupDescription: "Startup:"

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\*.pri"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\*.winmd"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\publish\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion skipifsourcedoesntexist recursesubdirs
Source: "..\publish\**"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist recursesubdirs createallsubdirs; Excludes: "{#MyAppExeName}"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "md2loop"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupentry

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
