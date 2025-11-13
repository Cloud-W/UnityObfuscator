using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Configuration for the Unity Obfuscator plugin
    /// </summary>
    [Serializable]
    public class ObfuscatorConfig
    {
        [Header("General Settings")]
        public bool enableObfuscation = true;
        public bool autoRunOnBuild = true;
        public bool enableBackup = true;
        public string backupPath = "Library/ObfuscatorBackups";
        
        [Header("Target Assemblies")]
        public string assemblyPath = "Library/ScriptAssemblies";
        public List<string> targetAssemblies = new List<string> { "Assembly-CSharp.dll" };
        
        [Header("Obfuscation Options")]
        public bool obfuscateTypeNames = true;
        public bool obfuscateMethodNames = true;
        public bool obfuscateFieldNames = true;
        public bool obfuscatePropertyNames = true;
        public bool encryptStrings = true;
        public bool controlFlowObfuscation = false;
        
        [Header("Unity Serialization Protection")]
        [Tooltip("Preserve MonoBehaviour and ScriptableObject class names (REQUIRED for scenes/prefabs)")]
        public bool preserveMonoBehaviourNames = true;
        [Tooltip("Preserve fields with [SerializeField] attribute (REQUIRED for Inspector serialization)")]
        public bool preserveSerializedFields = true;
        [Tooltip("EXPERIMENTAL: Attempt to patch asset files with obfuscated names. May cause build failures!")]
        public bool enableAssetPatching = false;
        
        [Header("Exclusions")]
        public List<string> excludedNamespaces = new List<string> 
        { 
            "UnityEngine",
            "UnityEditor",
            "System",
            "Mono"
        };
        
        public List<string> excludedAssemblies = new List<string>
        {
            "UnityEngine",
            "UnityEditor",
            "mscorlib",
            "System",
            "Mono.Cecil"
        };
        
        public List<string> excludedTypes = new List<string>();
        
        [Header("Whitelist Attributes")]
        public List<string> preserveAttributes = new List<string>
        {
            "UnityEngine.SerializeField",
            "UnityEngine.Serializable",
            "UnityEngine.Preserve",
            "System.Runtime.CompilerServices.CompilerGenerated"
        };
        
        [Header("String Encryption")]
        public string encryptionKey = "";
        public bool autoGenerateKey = true;
        
        [Header("Logging")]
        public bool verboseLogging = true;
        public bool generateReport = true;
        public string reportPath = "Library/ObfuscatorReports";
        
        [Header("Commercial Obfuscator Integration")]
        public bool useExternalObfuscator = false;
        public string externalObfuscatorPath = "";
        public string externalObfuscatorArgs = "";
        
        /// <summary>
        /// Load configuration from JSON file
        /// </summary>
        public static ObfuscatorConfig LoadFromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<ObfuscatorConfig>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load obfuscator config: {e.Message}");
                return CreateDefault();
            }
        }
        
        /// <summary>
        /// Save configuration to JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        /// <summary>
        /// Create default configuration
        /// </summary>
        public static ObfuscatorConfig CreateDefault()
        {
            return new ObfuscatorConfig();
        }
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool Validate(out string error)
        {
            error = "";
            
            if (enableObfuscation && targetAssemblies.Count == 0)
            {
                error = "No target assemblies specified";
                return false;
            }
            
            if (encryptStrings && !autoGenerateKey && string.IsNullOrEmpty(encryptionKey))
            {
                error = "String encryption enabled but no encryption key provided";
                return false;
            }
            
            if (useExternalObfuscator && string.IsNullOrEmpty(externalObfuscatorPath))
            {
                error = "External obfuscator enabled but path not specified";
                return false;
            }
            
            return true;
        }
    }
}
