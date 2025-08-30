# Debug run with terminal logging enabled
param(
    [string]$LogSuffix = ""
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFileName = if ($LogSuffix) { "terminal-debug-$LogSuffix-$timestamp.log" } else { "terminal-debug-$timestamp.log" }
$logPath = "D:\ClaudeCode\project\MovieMonitor\build\Debug\net8.0-windows\logs\$logFileName"

Write-Host "Starting MovieMonitor with terminal logging enabled" -ForegroundColor Green
Write-Host "Terminal log will be saved to: $logPath" -ForegroundColor Yellow
Write-Host ""

& "D:\ClaudeCode\project\MovieMonitor\run_debug.ps1" -LogTerminal -LogFile $logPath

Write-Host ""
Write-Host "Terminal log saved to: $logPath" -ForegroundColor Green

# ログファイルの最後の20行を表示
if (Test-Path $logPath) {
    Write-Host ""
    Write-Host "=== Last 20 lines of terminal log ===" -ForegroundColor Cyan
    Get-Content $logPath -Tail 20 | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
    Write-Host "=====================================" -ForegroundColor Cyan
}