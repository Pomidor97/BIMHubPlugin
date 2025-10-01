using System;
using System.IO;

namespace BIMHubPlugin.Services
{
    public static class SimpleLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BIMHubPlugin",
            "Logs",
            $"log_{DateTime.Now:yyyy-MM-dd}.txt"
        );

        static SimpleLogger()
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static void Log(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                File.AppendAllText(LogPath, logMessage + Environment.NewLine);
            }
            catch { }
        }

        public static void Error(string message, Exception ex)
        {
            Log($"ERROR: {message} | Exception: {ex.Message} | StackTrace: {ex.StackTrace}");
        }
    }
}