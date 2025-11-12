using UnityEditor;
using UnityEngine;
using System.IO;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Command-line interface for Unity Obfuscator
    /// Can be called from CI/CD pipelines using Unity's batch mode
    /// </summary>
    public static class ObfuscatorCLI
    {
        private const string CONFIG_FILE = "Assets/UnityObfuscator/Config/obfuscator_config.json";
        
        /// <summary>
        /// Run obfuscation from command line
        /// Usage: Unity.exe -quit -batchmode -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation
        /// Optional: -configPath "path/to/config.json"
        /// </summary>
        public static void RunObfuscation()
        {
            Debug.Log("[UnityObfuscator CLI] Starting obfuscation from command line");
            
            // Get config path from command line arguments
            string configPath = GetCommandLineArgument("-configPath") ?? CONFIG_FILE;
            
            // Load configuration
            ObfuscatorConfig config = LoadConfiguration(configPath);
            
            if (!config.enableObfuscation)
            {
                Debug.Log("[UnityObfuscator CLI] Obfuscation is disabled in configuration");
                EditorApplication.Exit(0);
                return;
            }
            
            // Create logger
            string logPath = Path.Combine(config.reportPath, $"cli_obfuscation_{System.DateTime.Now:yyyyMMdd_HHmmss}.log");
            var logger = new ObfuscatorLogger(config.verboseLogging, logPath);
            
            logger.Log("Running obfuscation from CLI");
            logger.Log($"Config file: {configPath}");
            
            // Run obfuscation
            var runner = new ObfuscatorRunner(config, logger);
            bool success = runner.Run();
            
            logger.SaveToFile();
            
            if (success)
            {
                Debug.Log("[UnityObfuscator CLI] Obfuscation completed successfully");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[UnityObfuscator CLI] Obfuscation failed");
                EditorApplication.Exit(1);
            }
        }
        
        /// <summary>
        /// Rollback obfuscation from command line
        /// Usage: Unity.exe -quit -batchmode -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.Rollback
        /// </summary>
        public static void Rollback()
        {
            Debug.Log("[UnityObfuscator CLI] Starting rollback from command line");
            
            // Get config path from command line arguments
            string configPath = GetCommandLineArgument("-configPath") ?? CONFIG_FILE;
            
            // Load configuration
            ObfuscatorConfig config = LoadConfiguration(configPath);
            
            var logger = new ObfuscatorLogger(config.verboseLogging);
            var runner = new ObfuscatorRunner(config, logger);
            
            bool success = runner.Rollback();
            
            if (success)
            {
                Debug.Log("[UnityObfuscator CLI] Rollback completed successfully");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[UnityObfuscator CLI] Rollback failed");
                EditorApplication.Exit(1);
            }
        }
        
        /// <summary>
        /// Generate default configuration file
        /// Usage: Unity.exe -quit -batchmode -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.GenerateConfig
        /// </summary>
        public static void GenerateConfig()
        {
            Debug.Log("[UnityObfuscator CLI] Generating default configuration");
            
            string configPath = GetCommandLineArgument("-configPath") ?? CONFIG_FILE;
            
            try
            {
                string directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var config = ObfuscatorConfig.CreateDefault();
                string json = config.ToJson();
                File.WriteAllText(configPath, json);
                
                Debug.Log($"[UnityObfuscator CLI] Configuration saved to: {configPath}");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UnityObfuscator CLI] Failed to generate config: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }
        
        /// <summary>
        /// Load configuration from file
        /// </summary>
        private static ObfuscatorConfig LoadConfiguration(string configPath)
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return ObfuscatorConfig.LoadFromJson(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UnityObfuscator CLI] Failed to load config: {ex.Message}");
                }
            }
            
            Debug.LogWarning($"[UnityObfuscator CLI] Config file not found: {configPath}, using defaults");
            return ObfuscatorConfig.CreateDefault();
        }
        
        /// <summary>
        /// Get a command line argument value
        /// </summary>
        private static string GetCommandLineArgument(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
