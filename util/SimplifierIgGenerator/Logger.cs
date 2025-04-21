// Logger.cs
using System;
using System.IO;
using System.Text;

public static class Logger
{
    private static StreamWriter? _logStreamWriter;
    private static readonly object _lock = new object(); // For potential future thread safety

    public static bool IsInitialized { get; private set; } = false;

    public static void Init(string logFilePath)
    {
        try
        {
            // Ensure the directory exists
            string? logDirectory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Open the file stream for appending, create if not exists
            // Using UTF8 encoding without BOM for better compatibility
            _logStreamWriter = new StreamWriter(logFilePath, append: true, encoding: new UTF8Encoding(false));
            _logStreamWriter.AutoFlush = true; // Ensure writes happen promptly

            IsInitialized = true;
            Log(LogLevel.Info, $"--- Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---", ConsoleColor.Gray);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FATAL: Failed to initialize file logger at {logFilePath}: {ex.Message}");
            Console.ResetColor();
            IsInitialized = false;
        }
    }

    public static void Close()
    {
        if (_logStreamWriter != null)
        {
            Log(LogLevel.Info, $"--- Log ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---", ConsoleColor.Gray);
            lock (_lock)
            {
                _logStreamWriter?.Flush();
                _logStreamWriter?.Close();
                _logStreamWriter = null;
            }
            IsInitialized = false;
            Console.WriteLine("Log file closed.");
        }
    }

    // Public logging methods
    public static void Info(string message) => Log(LogLevel.Info, message, Console.ForegroundColor); 
    public static void Success(string message) => Log(LogLevel.Success, message, ConsoleColor.Green);
    public static void Warning(string message) => Log(LogLevel.Warning, message, ConsoleColor.Yellow);
    public static void Error(string message) => Log(LogLevel.Error, message, ConsoleColor.Red);
    public static void Magenta(string message) => Log(LogLevel.Info, message, ConsoleColor.Magenta); 
    public static void Cyan(string message) => Log(LogLevel.Info, message, ConsoleColor.Cyan);   


    private enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    private static void Log(LogLevel level, string message, ConsoleColor consoleColor)
    {
        string formattedMessage = $"[{level.ToString().ToUpper()}] {message}";
        string timestampedMessage = $"{DateTime.Now:HH:mm:ss.fff} {formattedMessage}";

        lock (_lock) // Lock ensures console and file writes are somewhat atomic if threading were added
        {
            // Write to Console
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(message); // Keep console output clean without level/timestamp
            Console.ForegroundColor = originalColor;

            // Write to File (if initialized)
            if (_logStreamWriter != null && IsInitialized)
            {
                try
                {
                    _logStreamWriter.WriteLine(timestampedMessage);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if trying to write after stream closed
                    IsInitialized = false; // Mark as not initialized if error occurs
                }
                 catch (Exception ex)
                 {
                     // Report file write error to console
                     Console.ForegroundColor = ConsoleColor.DarkRed;
                     Console.WriteLine($"! Logger File Write Error: {ex.Message} !");
                     Console.ForegroundColor = originalColor;
                     IsInitialized = false; // Stop trying to write to file on error
                 }
            }
        }
    }
}