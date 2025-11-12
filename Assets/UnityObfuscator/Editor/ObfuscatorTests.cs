using UnityEngine;
using UnityEditor;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Simple integration tests for the obfuscator
    /// </summary>
    public static class ObfuscatorTests
    {
        [MenuItem("Tools/Unity Obfuscator/Run Tests")]
        public static void RunAllTests()
        {
            Debug.Log("[ObfuscatorTests] Starting tests...");
            
            int passed = 0;
            int failed = 0;
            
            // Test 1: Configuration validation
            if (TestConfigurationValidation())
            {
                Debug.Log("[ObfuscatorTests] ✓ Configuration validation test passed");
                passed++;
            }
            else
            {
                Debug.LogError("[ObfuscatorTests] ✗ Configuration validation test failed");
                failed++;
            }
            
            // Test 2: String encryption
            if (TestStringEncryption())
            {
                Debug.Log("[ObfuscatorTests] ✓ String encryption test passed");
                passed++;
            }
            else
            {
                Debug.LogError("[ObfuscatorTests] ✗ String encryption test failed");
                failed++;
            }
            
            // Test 3: Backup manager
            if (TestBackupManager())
            {
                Debug.Log("[ObfuscatorTests] ✓ Backup manager test passed");
                passed++;
            }
            else
            {
                Debug.LogError("[ObfuscatorTests] ✗ Backup manager test failed");
                failed++;
            }
            
            // Test 4: Logger
            if (TestLogger())
            {
                Debug.Log("[ObfuscatorTests] ✓ Logger test passed");
                passed++;
            }
            else
            {
                Debug.LogError("[ObfuscatorTests] ✗ Logger test failed");
                failed++;
            }
            
            Debug.Log($"[ObfuscatorTests] Tests completed: {passed} passed, {failed} failed");
            
            if (failed > 0)
            {
                EditorUtility.DisplayDialog("Tests Failed", 
                    $"{failed} test(s) failed. Check the console for details.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Tests Passed", 
                    $"All {passed} tests passed successfully!", "OK");
            }
        }
        
        private static bool TestConfigurationValidation()
        {
            try
            {
                // Test valid config
                var config = ObfuscatorConfig.CreateDefault();
                if (!config.Validate(out string error))
                {
                    Debug.LogError($"Default config validation failed: {error}");
                    return false;
                }
                
                // Test invalid config (no target assemblies)
                config.targetAssemblies.Clear();
                if (config.Validate(out error))
                {
                    Debug.LogError("Should have failed validation with no target assemblies");
                    return false;
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
        
        private static bool TestStringEncryption()
        {
            try
            {
                string key = StringEncryption.GenerateKey();
                
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogError("Failed to generate encryption key");
                    return false;
                }
                
                string plainText = "Hello, World! This is a test string.";
                string encrypted = StringEncryption.Encrypt(plainText, key);
                
                if (string.IsNullOrEmpty(encrypted) || encrypted == plainText)
                {
                    Debug.LogError("Encryption failed");
                    return false;
                }
                
                string decrypted = StringEncryption.Decrypt(encrypted, key);
                
                if (decrypted != plainText)
                {
                    Debug.LogError($"Decryption failed. Expected: '{plainText}', Got: '{decrypted}'");
                    return false;
                }
                
                // Test empty string
                string emptyEncrypted = StringEncryption.Encrypt("", key);
                if (emptyEncrypted != "")
                {
                    Debug.LogError("Empty string encryption failed");
                    return false;
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
        
        private static bool TestBackupManager()
        {
            try
            {
                var logger = new ObfuscatorLogger(false);
                var backupManager = new BackupManager("/tmp/test_backups", logger);
                
                // Create a backup folder
                string backupFolder = backupManager.CreateBackupFolder();
                
                if (string.IsNullOrEmpty(backupFolder))
                {
                    Debug.LogError("Failed to create backup folder");
                    return false;
                }
                
                if (!System.IO.Directory.Exists(backupFolder))
                {
                    Debug.LogError("Backup folder was not created");
                    return false;
                }
                
                // Clean up
                System.IO.Directory.Delete(backupFolder);
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
        
        private static bool TestLogger()
        {
            try
            {
                string logPath = "/tmp/test_obfuscator.log";
                var logger = new ObfuscatorLogger(false, logPath);
                
                logger.Log("Test message");
                logger.LogWarning("Test warning");
                logger.LogError("Test error");
                
                string fullLog = logger.GetFullLog();
                
                if (!fullLog.Contains("Test message") || 
                    !fullLog.Contains("Test warning") || 
                    !fullLog.Contains("Test error"))
                {
                    Debug.LogError("Logger did not capture all messages");
                    return false;
                }
                
                logger.SaveToFile();
                
                if (!System.IO.File.Exists(logPath))
                {
                    Debug.LogError("Log file was not saved");
                    return false;
                }
                
                // Clean up
                System.IO.File.Delete(logPath);
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
    }
}
