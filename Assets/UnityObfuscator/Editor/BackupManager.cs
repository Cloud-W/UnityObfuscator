using System;
using System.IO;
using UnityEngine;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Manages backup and rollback of assemblies
    /// </summary>
    public class BackupManager
    {
        private string backupRootPath;
        private ObfuscatorLogger logger;
        
        public BackupManager(string backupPath, ObfuscatorLogger logger)
        {
            this.backupRootPath = backupPath;
            this.logger = logger;
        }
        
        /// <summary>
        /// Create a timestamped backup folder
        /// </summary>
        public string CreateBackupFolder()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFolder = Path.Combine(backupRootPath, timestamp);
            
            try
            {
                Directory.CreateDirectory(backupFolder);
                logger.Log($"Created backup folder: {backupFolder}");
                return backupFolder;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to create backup folder: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Backup a single assembly file
        /// </summary>
        public bool BackupAssembly(string assemblyPath, string backupFolder)
        {
            try
            {
                if (!File.Exists(assemblyPath))
                {
                    logger.LogWarning($"Assembly not found for backup: {assemblyPath}");
                    return false;
                }
                
                string fileName = Path.GetFileName(assemblyPath);
                string backupPath = Path.Combine(backupFolder, fileName);
                
                File.Copy(assemblyPath, backupPath, true);
                
                // Also backup .pdb and .mdb files if they exist
                string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                if (File.Exists(pdbPath))
                {
                    File.Copy(pdbPath, Path.ChangeExtension(backupPath, ".pdb"), true);
                }
                
                string mdbPath = assemblyPath + ".mdb";
                if (File.Exists(mdbPath))
                {
                    File.Copy(mdbPath, backupPath + ".mdb", true);
                }
                
                logger.Log($"Backed up assembly: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to backup assembly {assemblyPath}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Restore an assembly from backup
        /// </summary>
        public bool RestoreAssembly(string backupFolder, string assemblyName, string targetPath)
        {
            try
            {
                string backupFile = Path.Combine(backupFolder, assemblyName);
                
                if (!File.Exists(backupFile))
                {
                    logger.LogError($"Backup file not found: {backupFile}");
                    return false;
                }
                
                string targetFile = Path.Combine(targetPath, assemblyName);
                File.Copy(backupFile, targetFile, true);
                
                // Restore .pdb and .mdb files if they exist
                string backupPdb = Path.ChangeExtension(backupFile, ".pdb");
                if (File.Exists(backupPdb))
                {
                    File.Copy(backupPdb, Path.ChangeExtension(targetFile, ".pdb"), true);
                }
                
                string backupMdb = backupFile + ".mdb";
                if (File.Exists(backupMdb))
                {
                    File.Copy(backupMdb, targetFile + ".mdb", true);
                }
                
                logger.Log($"Restored assembly: {assemblyName}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to restore assembly {assemblyName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get the most recent backup folder
        /// </summary>
        public string GetLatestBackupFolder()
        {
            try
            {
                if (!Directory.Exists(backupRootPath))
                {
                    logger.LogWarning("No backups found");
                    return null;
                }
                
                var directories = Directory.GetDirectories(backupRootPath);
                if (directories.Length == 0)
                {
                    logger.LogWarning("No backup folders found");
                    return null;
                }
                
                Array.Sort(directories);
                return directories[directories.Length - 1];
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to get latest backup: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Clean up old backups, keeping only the specified number
        /// </summary>
        public void CleanOldBackups(int keepCount = 5)
        {
            try
            {
                if (!Directory.Exists(backupRootPath))
                {
                    return;
                }
                
                var directories = Directory.GetDirectories(backupRootPath);
                if (directories.Length <= keepCount)
                {
                    return;
                }
                
                Array.Sort(directories);
                int toDelete = directories.Length - keepCount;
                
                for (int i = 0; i < toDelete; i++)
                {
                    Directory.Delete(directories[i], true);
                    logger.Log($"Deleted old backup: {Path.GetFileName(directories[i])}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to clean old backups: {ex.Message}");
            }
        }
    }
}
