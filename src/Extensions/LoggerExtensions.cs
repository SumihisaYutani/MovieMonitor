using Serilog;
using System.Runtime.CompilerServices;

namespace MovieMonitor.Extensions;

/// <summary>
/// ログ出力の拡張メソッド（ファイル名・行番号付き）
/// </summary>
public static class LoggerExtensions
{
    public static void LogInfoWithLocation(this ILogger logger, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath);
        logger.Information("[{FileName}:{LineNumber}] {MemberName}: {Message}", 
            fileName, lineNumber, memberName, message);
    }

    public static void LogErrorWithLocation(this ILogger logger, Exception exception, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath);
        logger.Error(exception, "[{FileName}:{LineNumber}] {MemberName}: {Message}", 
            fileName, lineNumber, memberName, message);
    }

    public static void LogWarningWithLocation(this ILogger logger, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath);
        logger.Warning("[{FileName}:{LineNumber}] {MemberName}: {Message}", 
            fileName, lineNumber, memberName, message);
    }

    public static void LogDebugWithLocation(this ILogger logger, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath);
        logger.Debug("[{FileName}:{LineNumber}] {MemberName}: {Message}", 
            fileName, lineNumber, memberName, message);
    }

    public static void LogFatalWithLocation(this ILogger logger, Exception exception, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = Path.GetFileName(filePath);
        logger.Fatal(exception, "[{FileName}:{LineNumber}] {MemberName}: {Message}", 
            fileName, lineNumber, memberName, message);
    }
}