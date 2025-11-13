using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Build hook that automatically runs obfuscation during Unity builds
    /// </summary>
    public class ObfuscatorBuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        private const string CONFIG_FILE = "Assets/UnityObfuscator/Config/obfuscator_config.json";
        
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[UnityObfuscator] Build preprocessing started");
            
            // Load configuration
            ObfuscatorConfig config = LoadConfiguration();
            
            if (!config.autoRunOnBuild)
            {
                Debug.Log("[UnityObfuscator] Auto-run on build is disabled");
                return;
            }
            
            if (!config.enableObfuscation)
            {
                Debug.Log("[UnityObfuscator] Obfuscation is disabled");
                return;
            }
            
            // Create logger
            string logPath = Path.Combine(config.reportPath, $"build_obfuscation_{System.DateTime.Now:yyyyMMdd_HHmmss}.log");
            var logger = new ObfuscatorLogger(config.verboseLogging, logPath);
            
            logger.Log($"Build target: {report.summary.platform}");
            logger.Log($"Build path: {report.summary.outputPath}");
            
            // Run obfuscation
            var runner = new ObfuscatorRunner(config, logger);
            bool success = runner.Run();
            
            logger.SaveToFile();
            
            if (!success)
            {
                Debug.LogError("[UnityObfuscator] Obfuscation failed during build");
                
                // Optionally fail the build
                if (EditorUtility.DisplayDialog(
                    "Obfuscation Failed",
                    "Obfuscation failed during build. Continue building anyway?",
                    "Continue", "Cancel Build"))
                {
                    Debug.LogWarning("[UnityObfuscator] Continuing build despite obfuscation failure");
                }
                else
                {
                    throw new BuildFailedException("[UnityObfuscator] Build cancelled due to obfuscation failure");
                }
            }
            else
            {
                Debug.Log("[UnityObfuscator] Obfuscation completed successfully");
            }
        }
        
        /// <summary>
        /// Load configuration from file or create default
        /// </summary>
        private ObfuscatorConfig LoadConfiguration()
        {
            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    string json = File.ReadAllText(CONFIG_FILE);
                    return ObfuscatorConfig.LoadFromJson(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UnityObfuscator] Failed to load config: {ex.Message}");
                }
            }
            
            // Return default configuration
            return ObfuscatorConfig.CreateDefault();
        }
    }
}
