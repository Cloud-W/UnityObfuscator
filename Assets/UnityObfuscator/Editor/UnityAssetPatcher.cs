using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Patches Unity asset files (scenes, prefabs) to update obfuscated type and field references
    /// </summary>
    public class UnityAssetPatcher
    {
        private ObfuscatorConfig config;
        private ObfuscatorLogger logger;
        private Dictionary<string, string> typeNameMap;
        private Dictionary<string, Dictionary<string, string>> fieldNameMaps; // TypeName -> FieldName -> ObfuscatedFieldName
        
        public UnityAssetPatcher(ObfuscatorConfig config, ObfuscatorLogger logger)
        {
            this.config = config;
            this.logger = logger;
            this.typeNameMap = new Dictionary<string, string>();
            this.fieldNameMaps = new Dictionary<string, Dictionary<string, string>>();
        }
        
        /// <summary>
        /// Register a type name mapping
        /// </summary>
        public void RegisterTypeMapping(string originalName, string obfuscatedName)
        {
            if (!string.IsNullOrEmpty(originalName) && !string.IsNullOrEmpty(obfuscatedName))
            {
                typeNameMap[originalName] = obfuscatedName;
                logger.Log($"Registered type mapping: {originalName} -> {obfuscatedName}");
            }
        }
        
        /// <summary>
        /// Register a field name mapping for a specific type
        /// </summary>
        public void RegisterFieldMapping(string typeName, string originalFieldName, string obfuscatedFieldName)
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(originalFieldName) || string.IsNullOrEmpty(obfuscatedFieldName))
                return;
                
            if (!fieldNameMaps.ContainsKey(typeName))
            {
                fieldNameMaps[typeName] = new Dictionary<string, string>();
            }
            
            fieldNameMaps[typeName][originalFieldName] = obfuscatedFieldName;
            logger.Log($"Registered field mapping: {typeName}.{originalFieldName} -> {obfuscatedFieldName}");
        }
        
        /// <summary>
        /// Patch all Unity asset files (scenes and prefabs) with obfuscated names
        /// </summary>
        public bool PatchAllAssets()
        {
            if (typeNameMap.Count == 0 && fieldNameMaps.Count == 0)
            {
                logger.Log("No mappings to apply, skipping asset patching");
                return true;
            }
            
            logger.Log("=== Starting Unity Asset Patching ===");
            logger.Log($"Type mappings: {typeNameMap.Count}");
            logger.Log($"Field mappings: {fieldNameMaps.Count}");
            
            try
            {
                // Find all scene and prefab files
                List<string> assetFiles = new List<string>();
                
                string assetsPath = "Assets";
                if (Directory.Exists(assetsPath))
                {
                    assetFiles.AddRange(Directory.GetFiles(assetsPath, "*.unity", SearchOption.AllDirectories));
                    assetFiles.AddRange(Directory.GetFiles(assetsPath, "*.prefab", SearchOption.AllDirectories));
                    assetFiles.AddRange(Directory.GetFiles(assetsPath, "*.asset", SearchOption.AllDirectories));
                }
                
                logger.Log($"Found {assetFiles.Count} asset files to patch");
                
                int patchedCount = 0;
                int errorCount = 0;
                
                foreach (var assetFile in assetFiles)
                {
                    try
                    {
                        if (PatchAssetFile(assetFile))
                        {
                            patchedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to patch {assetFile}: {ex.Message}");
                        errorCount++;
                    }
                }
                
                logger.Log($"=== Asset Patching Complete: {patchedCount} files patched, {errorCount} errors ===");
                return errorCount == 0;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return false;
            }
        }
        
        /// <summary>
        /// Patch a single Unity asset file (scene, prefab, or ScriptableObject)
        /// </summary>
        private bool PatchAssetFile(string assetPath)
        {
            try
            {
                // Read the asset file as text (Unity uses YAML format)
                string content = File.ReadAllText(assetPath, Encoding.UTF8);
                string originalContent = content;
                bool modified = false;
                
                // Pattern 1: MonoBehaviour script references
                // Example: m_Script: {fileID: 11500000, guid: xxxxx, type: 3}
                // We need to update the MonoBehaviour class name in the serialized data
                
                // Pattern 2: Serialized field names in YAML
                // Example: playerName: "John"
                // These appear as keys in the YAML structure
                
                foreach (var typeMapping in typeNameMap)
                {
                    string originalType = typeMapping.Key;
                    string obfuscatedType = typeMapping.Value;
                    
                    // Update MonoBehaviour references in component headers
                    // Pattern: "MonoBehaviour:" followed by the type name in comments or metadata
                    string typePattern = $@"\b{Regex.Escape(originalType)}\b";
                    if (Regex.IsMatch(content, typePattern))
                    {
                        content = Regex.Replace(content, typePattern, obfuscatedType);
                        modified = true;
                        logger.Log($"  Patched type reference: {originalType} -> {obfuscatedType} in {Path.GetFileName(assetPath)}");
                    }
                }
                
                // Patch field names
                // Unity YAML format: "  fieldName: value"
                foreach (var typeMappings in fieldNameMaps)
                {
                    foreach (var fieldMapping in typeMappings.Value)
                    {
                        string originalField = fieldMapping.Key;
                        string obfuscatedField = fieldMapping.Value;
                        
                        // Match field names that are keys in YAML (with proper indentation and colon)
                        string fieldPattern = $@"(^|\n)([ ]+){Regex.Escape(originalField)}:";
                        if (Regex.IsMatch(content, fieldPattern, RegexOptions.Multiline))
                        {
                            content = Regex.Replace(content, fieldPattern, $"$1$2{obfuscatedField}:", RegexOptions.Multiline);
                            modified = true;
                            logger.Log($"  Patched field reference: {originalField} -> {obfuscatedField} in {Path.GetFileName(assetPath)}");
                        }
                    }
                }
                
                // Write back if modified
                if (modified)
                {
                    // Create backup
                    string backupPath = assetPath + ".backup";
                    File.WriteAllText(backupPath, originalContent, Encoding.UTF8);
                    
                    // Write modified content
                    File.WriteAllText(assetPath, content, Encoding.UTF8);
                    
                    logger.Log($"Patched asset file: {assetPath}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error patching asset {assetPath}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get statistics about the mappings
        /// </summary>
        public string GetStatistics()
        {
            int totalFieldMappings = 0;
            foreach (var fields in fieldNameMaps.Values)
            {
                totalFieldMappings += fields.Count;
            }
            
            return $"Type mappings: {typeNameMap.Count}, Field mappings: {totalFieldMappings}";
        }
    }
}
