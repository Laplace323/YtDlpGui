; ======================================================
; YtDlpGui インストーラースクリプト (Inno Setup)
;
; 使い方：
; 1. まず dotnet publish で自己完結exeを生成する
;      dotnet publish -c Release -r win-x64 --self-contained true
;        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
;        -o publish
;    （YtDlpGui.Desktop プロジェクトのフォルダで実行）
;
; 2. 下の [Setup] SourceDir / [Files] Source を、
;    実際のフォルダ構成に合わせて調整する
;    （★の箇所を要確認）
;
; 3. Inno Setup Compiler（ISCC.exe、または付属のGUI）で
;    このファイルをビルドすると、
;    YtDlpGuiSetup-1.0.0.exe が生成される
; ======================================================

#define MyAppName "YtDlpGui"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Laplace"
#define MyAppExeName "YtDlpGui.exe"

; ★ dotnet publish の出力先フォルダ
;    (YtDlpGui.Desktop\publish のような場所を想定。
;     実際のプロジェクトのフォルダ名に合わせて変更する)
#define PublishDir "..\YtDlpGui.Desktop\publish"

; ★ .icoファイルの場所
;    (コアプロジェクトのAssetsフォルダを想定)
#define IconFile "..\YtDlpGui\Assets\YtDlpGui.ico"

; ★ READMEの場所（このスクリプトと同じフォルダを想定）
#define ReadmeFile "README.md"


[Setup]
; ==================================================
; AppIdはこのアプリ固有のGUID。
; 一度決めたら変更しないこと
; （変更すると「別のアプリ」として扱われ、
; 　アップグレードインストールができなくなる）。
; 下記はサンプル値なので、実際にビルドする前に
; Inno Setup付属の「Tools > Generate GUID」等で
; 生成した値に置き換えることを推奨する。
; ==================================================
AppId={{8F2C1E4A-6B3D-4A1F-9C7E-2D5B8A9F1C3E}}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

; ==================================================
; アプリ自体が %LOCALAPPDATA% にのみ書き込む設計
; （管理者権限不要）なので、インストール先を
; ユーザーごとのフォルダにして管理者権限を
; 要求しないようにする。
; 全ユーザー共通の場所にインストールしたい場合は
; PrivilegesRequired=admin に変更し、
; DefaultDirNameも{autopf}のままにする。
; ==================================================
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir=.\output
OutputBaseFilename={#MyAppName}Setup-{#MyAppVersion}

SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#MyAppExeName}

Compression=lzma2
SolidCompression=yes

WizardStyle=modern
DisableProgramGroupPage=yes

; 既にインストール済みの場合、同じバージョンでも
; 上書きインストールできるようにする
AllowNoIcons=yes


[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"


[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"


[Files]
; ==================================================
; dotnet publish の出力フォルダを丸ごと含める
; （自己完結・単体exeなので基本的に1ファイルのみの
; 　はずだが、依存DLLが残るケースも考慮し
; 　フォルダごとコピーする）
; ==================================================
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "{#ReadmeFile}"; DestDir: "{app}"; Flags: ignoreversion


[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\README"; Filename: "{app}\{#ReadmeFile}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon


[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#ReadmeFile}"; Description: "READMEを表示する"; Flags: postinstall shellexec skipifsilent unchecked


; ==================================================
; アンインストール時の挙動について
;
; デフォルトでは{app}（インストール先）のみ削除され、
; %LOCALAPPDATA%\YtDlpGui （設定・yt-dlp/FFmpeg/Denoなど）
; は残る。これは「再インストールしても設定や
; ダウンロード済みツールが消えない」という利点になるため、
; 意図的に削除処理を入れていない。
;
; 完全にクリーンアンインストールしたい場合は、
; 下のコメントを外すと %LOCALAPPDATA%\YtDlpGui ごと
; 削除できる。
; ==================================================
; [UninstallDelete]
; Type: filesandordirs; Name: "{localappdata}\YtDlpGui"
