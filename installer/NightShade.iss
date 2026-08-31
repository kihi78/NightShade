; NightShade インストーラー定義（Inno Setup）
; ビルド: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\NightShade.iss

#define MyAppName "NightShade"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "NightShade"
#define MyAppExeName "NightShade.exe"

[Setup]
AppId={{90960603-2F3A-4C25-A19B-C29791EE2A0B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; ユーザーごとのフォルダにインストールするため管理者権限は不要
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=NightShade_Setup_1.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupicon"; Description: "Windows 起動時に自動的に NightShade を開始する"; GroupDescription: "追加のオプション:"; Flags: unchecked
Name: "desktopicon"; Description: "デスクトップにアイコンを作成する"; GroupDescription: "追加のアイコン:"; Flags: unchecked

[Files]
Source: "..\publish\NightShade.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\NightShade"
