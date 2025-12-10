' Beep Python Host Admin - Silent Launcher
' Launches the application without showing console window

Set objShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' Get script directory
strScriptPath = objFSO.GetParentFolderName(WScript.ScriptFullName)

' Launch batch file hidden
objShell.Run Chr(34) & strScriptPath & Chr(92) & "BeepPythonHost.bat" & Chr(34), 0, False

Set objShell = Nothing
Set objFSO = Nothing