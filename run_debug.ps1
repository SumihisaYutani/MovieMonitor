# Debug runner script with terminal logging
param(
    [switch]$LogTerminal = $false,
    [string]$LogFile = ""
)

$exePath = "D:\ClaudeCode\project\MovieMonitor\build\Debug\net8.0-windows\MovieMonitor.exe"
$workingDir = "D:\ClaudeCode\project\MovieMonitor\build\Debug\net8.0-windows"

# 作業ディレクトリ変更
cd $workingDir

# Debug出力をコンソールに表示
[System.Diagnostics.Debug]::Listeners.Clear()
[System.Diagnostics.Debug]::Listeners.Add((New-Object System.Diagnostics.ConsoleTraceListener))

Write-Host "==================== MovieMonitor Debug Run ====================" -ForegroundColor Green
Write-Host "Executable: $exePath" -ForegroundColor Cyan
Write-Host "Working Directory: $workingDir" -ForegroundColor Cyan
Write-Host "Start Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "Command Line Args: $args" -ForegroundColor Cyan

if ($LogTerminal -and $LogFile) {
    Write-Host "Terminal Output will be logged to: $LogFile" -ForegroundColor Yellow
}

Write-Host "=================================================================" -ForegroundColor Green
Write-Host ""

try {
    if ($LogTerminal -and $LogFile) {
        # ターミナル出力をファイルに記録
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        "[$timestamp] TERMINAL SESSION START - MovieMonitor Debug Run" | Out-File -FilePath $LogFile -Encoding UTF8 -Append
        "[$timestamp] Command: $exePath $args" | Out-File -FilePath $LogFile -Encoding UTF8 -Append
        "[$timestamp] Working Directory: $workingDir" | Out-File -FilePath $LogFile -Encoding UTF8 -Append
        "[$timestamp] PowerShell Version: $($PSVersionTable.PSVersion)" | Out-File -FilePath $LogFile -Encoding UTF8 -Append
        "" | Out-File -FilePath $LogFile -Encoding UTF8 -Append
        
        # アプリケーション実行（出力をTeeしてファイルにも記録）
        & $exePath $args 2>&1 | ForEach-Object {
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
            $line = "[$timestamp] $_"
            Write-Host $line
            $line | Out-File -FilePath $LogFile -Encoding UTF8 -Append
        }
        
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        "[$timestamp] TERMINAL SESSION END - Exit Code: $LASTEXITCODE" | Out-File -FilePath $LogFile -Encoding UTF8 -Append
    } else {
        # 通常実行
        & $exePath $args
    }
    
    $exitCode = $LASTEXITCODE
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host "MovieMonitor finished with exit code: $exitCode" -ForegroundColor $(if($exitCode -eq 0){"Green"}else{"Red"})
    Write-Host "End Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Red
    Write-Host "ERROR: Failed to run MovieMonitor" -ForegroundColor Red
    Write-Host "Error Message: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "=================================================================" -ForegroundColor Red
    exit 1
}