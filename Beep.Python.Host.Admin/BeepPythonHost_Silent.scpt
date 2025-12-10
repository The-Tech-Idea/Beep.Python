-- Beep Python Host Admin - Silent Launcher for macOS
-- Launches the application without showing terminal window

tell application "Finder"
	set scriptPath to POSIX path of (container of (path to me) as alias)
end tell

do shell script "cd " & quoted form of scriptPath & " && nohup ./BeepPythonHost.sh > /dev/null 2>&1 &"
