using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Logging utility for obfuscation operations
    /// </summary>
    public class ObfuscatorLogger
    {
        private StringBuilder logBuilder;
        private string logFilePath;
        private bool verboseLogging;
        
        public ObfuscatorLogger(bool verbose = true, string logPath = null)
        {
            verboseLogging = verbose;
            logBuilder = new StringBuilder();
            
            if (!string.IsNullOrEmpty(logPath))
            {
                logFilePath = logPath;
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            }
        }
        
        public void Log(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            logBuilder.AppendLine(timestampedMessage);
            
            if (verboseLogging)
            {
                Debug.Log($"[UnityObfuscator] {message}");
            }
        }
        
        public void LogWarning(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: {message}";
            logBuilder.AppendLine(timestampedMessage);
            Debug.LogWarning($"[UnityObfuscator] {message}");
        }
        
        public void LogError(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}";
            logBuilder.AppendLine(timestampedMessage);
            Debug.LogError($"[UnityObfuscator] {message}");
        }
        
        public void LogException(Exception ex)
        {
            string message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EXCEPTION: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            logBuilder.AppendLine(message);
            Debug.LogException(ex);
        }
        
        public string GetFullLog()
        {
            return logBuilder.ToString();
        }
        
        public void SaveToFile()
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    File.WriteAllText(logFilePath, logBuilder.ToString());
                    Debug.Log($"[UnityObfuscator] Log saved to: {logFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnityObfuscator] Failed to save log: {ex.Message}");
                }
            }
        }
        
        public void Clear()
        {
            logBuilder.Clear();
        }
    }
}
