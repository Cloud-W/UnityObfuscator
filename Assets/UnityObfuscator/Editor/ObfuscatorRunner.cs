using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Main orchestrator for assembly obfuscation
    /// </summary>
    public class ObfuscatorRunner
    {
        private ObfuscatorConfig config;
        private ObfuscatorLogger logger;
        private BackupManager backupManager;
        private string currentBackupFolder;
        
        public ObfuscatorRunner(ObfuscatorConfig config, ObfuscatorLogger logger)
        {
            this.config = config;
            this.logger = logger;
            this.backupManager = new BackupManager(config.backupPath, logger);
        }
        
        /// <summary>
        /// Run the obfuscation process
        /// </summary>
        public bool Run()
        {
            logger.Log("=== Starting Obfuscation Process ===");
            
            // Validate configuration
            if (!config.Validate(out string error))
            {
                logger.LogError($"Configuration validation failed: {error}");
                return false;
            }
            
            // Check if obfuscation is enabled
            if (!config.enableObfuscation)
            {
                logger.Log("Obfuscation is disabled in configuration");
                return true;
            }
            
            try
            {
                // Generate encryption key if needed
                if (config.encryptStrings && config.autoGenerateKey)
                {
                    config.encryptionKey = StringEncryption.GenerateKey();
                    logger.Log("Generated new encryption key");
                }
                
                // Create backup if enabled
                if (config.enableBackup)
                {
                    currentBackupFolder = backupManager.CreateBackupFolder();
                }
                
                // Get target assemblies
                List<string> assemblies = GetTargetAssemblies();
                if (assemblies.Count == 0)
                {
                    logger.LogWarning("No target assemblies found");
                    return false;
                }
                
                logger.Log($"Found {assemblies.Count} target assemblies");
                
                // Backup assemblies
                if (config.enableBackup)
                {
                    foreach (var assembly in assemblies)
                    {
                        backupManager.BackupAssembly(assembly, currentBackupFolder);
                    }
                }
                
                // Obfuscate each assembly
                int successCount = 0;
                foreach (var assembly in assemblies)
                {
                    logger.Log($"Processing assembly: {Path.GetFileName(assembly)}");
                    
                    if (config.useExternalObfuscator)
                    {
                        if (ObfuscateWithExternalTool(assembly))
                        {
                            successCount++;
                        }
                    }
                    else
                    {
                        if (ObfuscateWithBuiltIn(assembly))
                        {
                            successCount++;
                        }
                    }
                }
                
                logger.Log($"=== Obfuscation Complete: {successCount}/{assemblies.Count} assemblies processed ===");
                
                // Generate report
                if (config.generateReport)
                {
                    GenerateReport(assemblies, successCount);
                }
                
                // Clean old backups
                if (config.enableBackup)
                {
                    backupManager.CleanOldBackups(5);
                }
                
                return successCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                
                // Attempt rollback on error
                if (config.enableBackup && !string.IsNullOrEmpty(currentBackupFolder))
                {
                    logger.LogWarning("Attempting to rollback due to error...");
                    Rollback();
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// Get list of target assemblies to obfuscate
        /// </summary>
        private List<string> GetTargetAssemblies()
        {
            List<string> assemblies = new List<string>();
            
            if (!Directory.Exists(config.assemblyPath))
            {
                logger.LogWarning($"Assembly path not found: {config.assemblyPath}");
                return assemblies;
            }
            
            foreach (var targetName in config.targetAssemblies)
            {
                string assemblyPath = Path.Combine(config.assemblyPath, targetName);
                
                if (File.Exists(assemblyPath))
                {
                    // Check if assembly should be excluded
                    if (!ShouldExcludeAssembly(targetName))
                    {
                        assemblies.Add(assemblyPath);
                    }
                    else
                    {
                        logger.Log($"Excluded assembly: {targetName}");
                    }
                }
                else
                {
                    logger.LogWarning($"Target assembly not found: {assemblyPath}");
                }
            }
            
            return assemblies;
        }
        
        /// <summary>
        /// Check if an assembly should be excluded
        /// </summary>
        private bool ShouldExcludeAssembly(string assemblyName)
        {
            foreach (var excluded in config.excludedAssemblies)
            {
                if (assemblyName.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Obfuscate using external tool
        /// </summary>
        private bool ObfuscateWithExternalTool(string assemblyPath)
        {
            try
            {
                logger.Log($"Using external obfuscator: {config.externalObfuscatorPath}");
                
                string args = config.externalObfuscatorArgs.Replace("{assembly}", assemblyPath);
                
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = config.externalObfuscatorPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        logger.Log($"External obfuscator succeeded: {Path.GetFileName(assemblyPath)}");
                        if (!string.IsNullOrEmpty(output))
                        {
                            logger.Log($"Output: {output}");
                        }
                        return true;
                    }
                    else
                    {
                        logger.LogError($"External obfuscator failed with code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            logger.LogError($"Error: {error}");
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to run external obfuscator: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Obfuscate using built-in obfuscator
        /// NOTE: This requires Mono.Cecil library to be added to Assets/UnityObfuscator/Libs/
        /// Download from: https://www.nuget.org/packages/Mono.Cecil/
        /// </summary>
        private bool ObfuscateWithBuiltIn(string assemblyPath)
        {
            try
            {
                // Check if Mono.Cecil is available
                Type cecilType = Type.GetType("Mono.Cecil.AssemblyDefinition, Mono.Cecil");
                if (cecilType == null)
                {
                    logger.LogError("Mono.Cecil library not found. Please add Mono.Cecil.dll to Assets/UnityObfuscator/Libs/");
                    logger.LogError("Download from: https://www.nuget.org/packages/Mono.Cecil/");
                    logger.LogError("Or enable 'Use External Obfuscator' in the configuration.");
                    return false;
                }
                
                // Use CecilObfuscator for the actual obfuscation work
                var obfuscator = new CecilObfuscator(config, logger);
                return obfuscator.Obfuscate(assemblyPath);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return false;
            }
        }
        
        /// <summary>
        /// Rollback to the most recent backup
        /// </summary>
        public bool Rollback()
        {
            try
            {
                string backupFolder = currentBackupFolder ?? backupManager.GetLatestBackupFolder();
                
                if (string.IsNullOrEmpty(backupFolder))
                {
                    logger.LogError("No backup folder found for rollback");
                    return false;
                }
                
                logger.Log($"Rolling back from: {backupFolder}");
                
                var backupFiles = Directory.GetFiles(backupFolder, "*.dll");
                int restoredCount = 0;
                
                foreach (var backupFile in backupFiles)
                {
                    string fileName = Path.GetFileName(backupFile);
                    if (backupManager.RestoreAssembly(backupFolder, fileName, config.assemblyPath))
                    {
                        restoredCount++;
                    }
                }
                
                logger.Log($"Rollback complete: {restoredCount} assemblies restored");
                return restoredCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return false;
            }
        }
        
        /// <summary>
        /// Generate obfuscation report
        /// </summary>
        private void GenerateReport(List<string> assemblies, int successCount)
        {
            try
            {
                Directory.CreateDirectory(config.reportPath);
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string reportFile = Path.Combine(config.reportPath, $"obfuscation_report_{timestamp}.txt");
                
                using (var writer = new StreamWriter(reportFile))
                {
                    writer.WriteLine("Unity Obfuscator Report");
                    writer.WriteLine($"Generated: {DateTime.Now}");
                    writer.WriteLine($"Configuration: {config.ToJson()}");
                    writer.WriteLine();
                    writer.WriteLine($"Processed Assemblies: {successCount}/{assemblies.Count}");
                    writer.WriteLine();
                    writer.WriteLine("Target Assemblies:");
                    foreach (var assembly in assemblies)
                    {
                        writer.WriteLine($"  - {Path.GetFileName(assembly)}");
                    }
                    writer.WriteLine();
                    writer.WriteLine("Full Log:");
                    writer.WriteLine(logger.GetFullLog());
                }
                
                logger.Log($"Report generated: {reportFile}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to generate report: {ex.Message}");
            }
        }
    }
}
