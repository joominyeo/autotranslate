using System.IO;
using System.Net;
using System.Net.Sockets;

namespace AutoTranslate.Core
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "AutoTranslate", 
            "Logs");
        
        private static readonly string LogFileName = $"autotranslate_{DateTime.Now:yyyy-MM-dd}.log";
        private static readonly string LogFilePath = Path.Combine(LogDirectory, LogFileName);
        private static readonly object LogLock = new object();

        static Logger()
        {
            EnsureLogDirectoryExists();
            CleanupOldLogs();
        }

        private static void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch (Exception ex)
            {
                // If we can't create log directory, write to temp
                Console.WriteLine($"Failed to create log directory: {ex.Message}");
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var logFiles = Directory.GetFiles(LogDirectory, "autotranslate_*.log");
                var cutoffDate = DateTime.Now.AddDays(-30); // Keep logs for 30 days

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public static void Log(LogLevel level, string message, Exception exception = null)
        {
            try
            {
                lock (LogLock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] {message}";
                    
                    if (exception != null)
                    {
                        logEntry += $"\nException: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}";
                    }

                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);

                    // Also write to debug output in development
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // If logging fails, don't crash the application
            }
        }

        public static void Debug(string message) => Log(LogLevel.Debug, message);
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public static void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);

        public static string GetLogFilePath() => LogFilePath;
        public static string GetLogDirectory() => LogDirectory;

        public static void LogStartup()
        {
            Info("=== AutoTranslate Application Started ===");
            Info($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Info($"OS: {Environment.OSVersion}");
            Info($".NET: {Environment.Version}");
            Info($"Working Directory: {Environment.CurrentDirectory}");
        }

        public static void LogShutdown()
        {
            Info("=== AutoTranslate Application Shutdown ===");
        }
    }

    public static class ErrorHandler
    {
        public static void HandleException(Exception exception, string context = "", bool showToUser = true)
        {
            Logger.Error($"Exception in {context}: {exception.Message}", exception);

            if (showToUser)
            {
                ShowUserFriendlyError(exception, context);
            }
        }

        public static void ShowUserFriendlyError(Exception exception, string context = "")
        {
            string userMessage = GetUserFriendlyMessage(exception, context);
            
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                System.Windows.MessageBox.Show(
                    userMessage, 
                    "AutoTranslate Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
            });
        }

        private static string GetUserFriendlyMessage(Exception exception, string context)
        {
            switch (exception)
            {
                case HttpRequestException:
                    return "Unable to connect to the translation service. Please check your internet connection and try again.";
                
                case TaskCanceledException:
                    return "The translation request timed out. Please try again or check your network connection.";
                
                case UnauthorizedAccessException when context.Contains("API"):
                    return "Invalid API key. Please check your Google Translate API key in Settings.";
                
                case FileNotFoundException:
                    return "A required file is missing. Please reinstall the application.";
                
                case DirectoryNotFoundException:
                    return "Unable to access application data directory. Please check file permissions.";
                
                case OutOfMemoryException:
                    return "The system is running low on memory. Please close other applications and try again.";
                
                case InvalidOperationException when exception.Message.Contains("hotkey"):
                    return "Unable to register the hotkey. It may already be in use by another application.";
                
                case ArgumentException when context.Contains("language"):
                    return "Invalid language selection. Please choose a valid language from the dropdown.";
                
                default:
                    return $"An unexpected error occurred: {exception.Message}\n\nFor more details, check the log file in Settings > Advanced > Open Config Folder.";
            }
        }

        public static bool ShouldRetry(Exception exception, int attemptCount, int maxAttempts)
        {
            if (attemptCount >= maxAttempts) return false;

            // Retry for network-related errors
            return exception is HttpRequestException ||
                   exception is TaskCanceledException ||
                   exception is SocketException ||
                   (exception is WebException webEx && IsRetryableWebException(webEx));
        }

        private static bool IsRetryableWebException(WebException webException)
        {
            return webException.Status == WebExceptionStatus.Timeout ||
                   webException.Status == WebExceptionStatus.ConnectFailure ||
                   webException.Status == WebExceptionStatus.ReceiveFailure ||
                   webException.Status == WebExceptionStatus.SendFailure;
        }
    }
}