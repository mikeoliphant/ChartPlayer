; ChartPlayer's Windows installer (Inno Setup 6), built by the Package workflow, which passes:
;   AppVersion  the version (ChartPlayerPlugin.csproj's)
;   SourceDir   the assembled folder: the application in ChartPlayer\, the plugin's bundles in clap\ and vst3\
;   OutputDir   where the installer goes

#ifndef AppVersion
  #error AppVersion isn't defined: pass /DAppVersion=<version> (the Package workflow does).
#endif

[Setup]
AppId={{3F7A2C94-6D1B-4E8F-A5C3-9B2D7E1F4A68}
AppName=ChartPlayer
AppVersion={#AppVersion}
AppPublisher=Mike Oliphant
AppPublisherURL=https://github.com/mikeoliphant/ChartPlayer
AppSupportURL=https://github.com/mikeoliphant/ChartPlayer/issues
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DefaultDirName={autopf}\ChartPlayer
DefaultGroupName=ChartPlayer
DisableProgramGroupPage=yes
; For everyone (needs administrator rights) or just the current user, as chosen.
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=..\ChartPlayer\Icon.ico
UninstallDisplayIcon={app}\ChartPlayer.exe
UninstallDisplayName=ChartPlayer
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
OutputDir={#OutputDir}
OutputBaseFilename=ChartPlayer-{#AppVersion}-setup
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "plugins"; Description: "The ChartPlayer plugin for DAWs (CLAP and VST3; needs the .NET 10 Desktop Runtime)"; GroupDescription: "Plugins:"

[Files]
Source: "{#SourceDir}\ChartPlayer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; The plugin goes where DAWs look for it: Common Files\CLAP and Common Files\VST3, in Program Files for everyone, or
; in %LOCALAPPDATA%\Programs\Common for the current user ({autocf64} is whichever the installation is).
Source: "{#SourceDir}\clap\*"; DestDir: "{autocf64}\CLAP"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: plugins
Source: "{#SourceDir}\vst3\*"; DestDir: "{autocf64}\VST3"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: plugins

[Icons]
Name: "{group}\ChartPlayer"; Filename: "{app}\ChartPlayer.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall ChartPlayer"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ChartPlayer"; Filename: "{app}\ChartPlayer.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\ChartPlayer.exe"; Description: "Start ChartPlayer"; Flags: postinstall nowait skipifsilent
