; =====================================================================
; Inno Setup Script for Turbo X - High Speed Download Accelerator
; Developed by Abir
; =====================================================================

#define MyAppName "Turbo X"
#define MyAppVersion "2.0"
#define MyAppPublisher "Abir"
#define MyAppURL "https://t.me/abrno34"
#define MyAppExeName "IDMTurboDownloader.exe"

[Setup]
AppId={{8B5C7D29-5E91-492B-9B21-5085C0A74F20}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Turbo X
DefaultGroupName=Turbo X
LicenseFile=license.txt
OutputDir=Output
OutputBaseFilename=TurboX_Setup
SetupIconFile=app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\app.ico
UninstallDisplayName=Turbo X - Download Accelerator & Browser

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "autostart"; Description: "Start Turbo X automatically when Windows starts (system tray)"; GroupDescription: "Windows Integration:"; Flags: checkedonce


[Files]
Source: "IDMTurboDownloader.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "aria2c.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "yt-dlp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "turbox_icon.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "license.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "extension\*"; DestDir: "{app}\extension"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "extension_firefox\*"; DestDir: "{app}\extension_firefox"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: startmenuicon
Name: "{autoprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; IconFilename: "{app}\app.ico"; Tasks: startmenuicon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TurboX"; ValueData: """{app}\{#MyAppExeName}"" --background"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; Flags: runhidden; Check: NeedsWebView2
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function NeedsWebView2(): Boolean;
var
  Version: String;
begin
  Result := True;
  // Check 64-bit registry
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
  begin
    if (Version <> '') and (Version <> '0.0.0.0') then
    begin
      Result := False;
      Exit;
    end;
  end;
  // Check 32-bit registry
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
  begin
    if (Version <> '') and (Version <> '0.0.0.0') then
    begin
      Result := False;
      Exit;
    end;
  end;
  // Check Current User registry
  if RegQueryStringValue(HKEY_CURRENT_USER, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
  begin
    if (Version <> '') and (Version <> '0.0.0.0') then
    begin
      Result := False;
      Exit;
    end;
  end;
end;
