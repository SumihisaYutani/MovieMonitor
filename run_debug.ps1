# Debug runner script
cd "D:\ClaudeCode\project\MovieMonitor\build\Debug\net8.0-windows"
[System.Diagnostics.Debug]::Listeners.Clear()
[System.Diagnostics.Debug]::Listeners.Add((New-Object System.Diagnostics.ConsoleTraceListener))
.\MovieMonitor.exe