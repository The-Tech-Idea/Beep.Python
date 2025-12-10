; Beep.Python Host Admin - Inno Setup Installer Script
; This creates a professional Windows installer with wizard

#define MyAppName "Beep.Python Host Admin"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "The Tech Idea"
#define MyAppURL "https://github.com/The-Tech-Idea/Beep.Python"
#define MyAppExeName "BeepPythonHost.exe"
#define MyAppDescription "Professional Python Environment & LLM Management System"

[Setup]
; Application identity
AppId={{B8E4F2A1-5C3D-4E6F-9A1B-2C3D4E5F6A7B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Default installation directory
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

; Allow user to change install location
DisableDirPage=no
DisableProgramGroupPage=no

; Output settings
OutputDir=..\dist\installer
OutputBaseFilename=BeepPythonHost-Setup-{#MyAppVersion}
SetupIconFile=..\assets\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Privileges (can install for current user only or all users)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Wizard settings
WizardStyle=modern
WizardSizePercent=120
DisableWelcomePage=no

; Version info
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}
VersionInfoProductName={#MyAppName}

; License and readme
LicenseFile=..\LICENSE
InfoBeforeFile=..\installer\readme_before.txt
InfoAfterFile=..\installer\readme_after.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full installation (recommended)"
Name: "compact"; Description: "Compact installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "main"; Description: "Beep.Python Host Admin Application"; Types: full compact custom; Flags: fixed
Name: "shortcuts"; Description: "Create Desktop and Start Menu shortcuts"; Types: full
Name: "autostart"; Description: "Start automatically with Windows"; Types: full

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Components: shortcuts
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Components: shortcuts; Flags: unchecked
Name: "startupicon"; Description: "Start Beep.Python when Windows starts"; GroupDescription: "Startup Options:"; Components: autostart; Flags: unchecked

[Dirs]
Name: "{app}\config"; Permissions: users-modify
Name: "{app}\data"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify

[Files]
; Main application files from PyInstaller output
Source: "..\dist\BeepPythonHost\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Configuration templates
Source: "..\config\*"; DestDir: "{app}\config"; Flags: ignoreversion recursesubdirs createallsubdirs onlyifdoesntexist

[Icons]
; Start Menu icons
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{#MyAppName} (Configure)"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--configure"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{group}\Open Data Folder"; Filename: "{app}\data"
Name: "{group}\View Logs"; Filename: "{app}\logs"

; Desktop icon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

; Quick Launch icon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: quicklaunchicon

; Startup icon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--minimized"; WorkingDir: "{app}"; Tasks: startupicon

[Registry]
; Store installation path for other applications to find
Root: HKCU; Subkey: "Software\TheTechIdea\BeepPython"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\TheTechIdea\BeepPython"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\TheTechIdea\BeepPython"; ValueType: string; ValueName: "DataPath"; ValueData: "{app}\data"; Flags: uninsdeletekey

[Run]
; Option to launch after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up log files on uninstall (but keep data by default)
Type: filesandordirs; Name: "{app}\logs"

[Code]
var
  ConfigPage: TInputDirWizardPage;
  PortPage: TInputQueryWizardPage;
  DataPath: String;
  ServerPort: String;

procedure InitializeWizard;
begin
  // Create custom page for data directory selection
  ConfigPage := CreateInputDirPage(wpSelectDir,
    'Select Data Directory',
    'Where should Beep.Python store its data files?',
    'This includes databases, downloaded models, and configuration files.' + #13#10 + #13#10 +
    'Select the folder where you want to store application data, then click Next.',
    False, 'New Folder');
  ConfigPage.Add('');
  ConfigPage.Values[0] := ExpandConstant('{userappdata}\BeepPython\data');

  // Create custom page for server configuration
  PortPage := CreateInputQueryPage(ConfigPage.ID,
    'Server Configuration',
    'Configure the web server settings',
    'Beep.Python runs a local web server for the management interface.');
  PortPage.Add('Server Port (default: 5000):', False);
  PortPage.Add('Server Host (default: 127.0.0.1):', False);
  PortPage.Values[0] := '5000';
  PortPage.Values[1] := '127.0.0.1';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Port: Integer;
begin
  Result := True;
  
  if CurPageID = ConfigPage.ID then
  begin
    DataPath := ConfigPage.Values[0];
    // Create the data directory if it doesn't exist
    if not DirExists(DataPath) then
    begin
      if not ForceDirectories(DataPath) then
      begin
        MsgBox('Could not create the data directory. Please select a different location.', mbError, MB_OK);
        Result := False;
      end;
    end;
  end;

  if CurPageID = PortPage.ID then
  begin
    ServerPort := PortPage.Values[0];
    // Validate port number
    Port := StrToIntDef(ServerPort, -1);
    if (Port < 1) or (Port > 65535) then
    begin
      MsgBox('Please enter a valid port number (1-65535).', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFile: String;
  ConfigContent: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Create initial configuration file with user's settings
    ConfigFile := ExpandConstant('{app}\config\install_config.json');
    ConfigContent := '{' + #13#10 +
      '  "data_path": "' + DataPath + '",' + #13#10 +
      '  "server_port": ' + ServerPort + ',' + #13#10 +
      '  "server_host": "' + PortPage.Values[1] + '",' + #13#10 +
      '  "installed_version": "' + ExpandConstant('{#MyAppVersion}') + '",' + #13#10 +
      '  "install_path": "' + ExpandConstant('{app}') + '"' + #13#10 +
      '}';
    SaveStringToFile(ConfigFile, ConfigContent, False);
    
    // Set environment variable for the data path
    RegWriteStringValue(HKEY_CURRENT_USER, 'Environment', 'BEEP_PYTHON_HOME', DataPath);
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := '';
  
  Result := Result + 'Installation Directory:' + NewLine;
  Result := Result + Space + ExpandConstant('{app}') + NewLine + NewLine;
  
  Result := Result + 'Data Directory:' + NewLine;
  Result := Result + Space + DataPath + NewLine + NewLine;
  
  Result := Result + 'Server Configuration:' + NewLine;
  Result := Result + Space + 'Port: ' + ServerPort + NewLine;
  Result := Result + Space + 'Host: ' + PortPage.Values[1] + NewLine + NewLine;
  
  if MemoComponentsInfo <> '' then
  begin
    Result := Result + 'Components:' + NewLine;
    Result := Result + MemoComponentsInfo + NewLine;
  end;
  
  if MemoTasksInfo <> '' then
  begin
    Result := Result + 'Additional Tasks:' + NewLine;
    Result := Result + MemoTasksInfo + NewLine;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := MsgBox('Are you sure you want to uninstall Beep.Python Host Admin?' + #13#10 + #13#10 +
    'Note: Your data files will be preserved in the data directory.', 
    mbConfirmation, MB_YESNO) = IDYES;
end;
