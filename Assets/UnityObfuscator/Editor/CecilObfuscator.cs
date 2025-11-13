using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Obfuscator implementation using Mono.Cecil
    /// This class uses reflection to work with Cecil dynamically
    /// </summary>
    public class CecilObfuscator
    {
        private ObfuscatorConfig config;
        private ObfuscatorLogger logger;
        private Dictionary<string, string> nameMap;
        private int nameCounter;
        private UnityAssetPatcher assetPatcher;
        private string currentTypeName; // Track current type being processed
        
        public CecilObfuscator(ObfuscatorConfig config, ObfuscatorLogger logger)
        {
            this.config = config;
            this.logger = logger;
            this.nameMap = new Dictionary<string, string>();
            this.nameCounter = 0;
            this.assetPatcher = new UnityAssetPatcher(config, logger);
            this.currentTypeName = null;
        }
        
        /// <summary>
        /// Get the asset patcher to apply mappings to Unity assets
        /// </summary>
        public UnityAssetPatcher GetAssetPatcher()
        {
            return assetPatcher;
        }
        
        /// <summary>
        /// Obfuscate an assembly using Mono.Cecil
        /// </summary>
        public bool Obfuscate(string assemblyPath)
        {
            try
            {
                logger.Log($"Obfuscating assembly: {Path.GetFileName(assemblyPath)}");
                
                // Load Cecil types dynamically
                Assembly cecilAssembly = GetCecilAssembly();
                if (cecilAssembly == null)
                {
                    logger.LogError("Could not load Mono.Cecil assembly");
                    return false;
                }
                
                // Get Cecil types
                Type assemblyDefinitionType = cecilAssembly.GetType("Mono.Cecil.AssemblyDefinition");
                Type moduleDefinitionType = cecilAssembly.GetType("Mono.Cecil.ModuleDefinition");
                Type typeDefinitionType = cecilAssembly.GetType("Mono.Cecil.TypeDefinition");
                Type methodDefinitionType = cecilAssembly.GetType("Mono.Cecil.MethodDefinition");
                Type fieldDefinitionType = cecilAssembly.GetType("Mono.Cecil.FieldDefinition");
                Type propertyDefinitionType = cecilAssembly.GetType("Mono.Cecil.PropertyDefinition");
                Type readerParametersType = cecilAssembly.GetType("Mono.Cecil.ReaderParameters");
                Type writerParametersType = cecilAssembly.GetType("Mono.Cecil.WriterParameters");
                
                if (assemblyDefinitionType == null)
                {
                    logger.LogError("Could not find Mono.Cecil.AssemblyDefinition type");
                    return false;
                }
                
                // Read assembly - use simple ReadAssembly without parameters first
                MethodInfo readAssembly = assemblyDefinitionType.GetMethod("ReadAssembly", 
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                    
                if (readAssembly == null)
                {
                    logger.LogError("Could not find ReadAssembly method");
                    return false;
                }
                
                object assembly = readAssembly.Invoke(null, new object[] { assemblyPath });
                if (assembly == null)
                {
                    logger.LogError($"Failed to read assembly: {assemblyPath}");
                    return false;
                }
                
                logger.Log("Assembly loaded successfully");
                
                // Get main module
                PropertyInfo mainModuleProperty = assemblyDefinitionType.GetProperty("MainModule");
                object mainModule = mainModuleProperty.GetValue(assembly);
                
                // Get types collection
                PropertyInfo typesProperty = moduleDefinitionType.GetProperty("Types");
                var types = typesProperty.GetValue(mainModule) as System.Collections.IEnumerable;
                
                int processedTypes = 0;
                int processedMembers = 0;
                
                // Process each type
                foreach (object type in types)
                {
                    string typeName = typeDefinitionType.GetProperty("FullName").GetValue(type) as string;
                    string simpleTypeName = typeDefinitionType.GetProperty("Name").GetValue(type) as string;
                    
                    // Set current type name for field mapping
                    currentTypeName = simpleTypeName;
                    
                    if (ShouldExcludeType(typeName))
                    {
                        continue;
                    }
                    
                    // Obfuscate type name
                    if (config.obfuscateTypeNames)
                    {
                        if (ObfuscateTypeName(type, typeDefinitionType))
                        {
                            processedTypes++;
                        }
                    }
                    
                    // Obfuscate methods
                    if (config.obfuscateMethodNames)
                    {
                        PropertyInfo methodsProperty = typeDefinitionType.GetProperty("Methods");
                        var methods = methodsProperty.GetValue(type) as System.Collections.IEnumerable;
                        
                        foreach (object method in methods)
                        {
                            if (ObfuscateMethodName(method, methodDefinitionType))
                            {
                                processedMembers++;
                            }
                        }
                    }
                    
                    // Obfuscate fields
                    if (config.obfuscateFieldNames)
                    {
                        PropertyInfo fieldsProperty = typeDefinitionType.GetProperty("Fields");
                        var fields = fieldsProperty.GetValue(type) as System.Collections.IEnumerable;
                        
                        foreach (object field in fields)
                        {
                            if (ObfuscateFieldName(field, fieldDefinitionType))
                            {
                                processedMembers++;
                            }
                        }
                    }
                    
                    // Obfuscate properties
                    if (config.obfuscatePropertyNames)
                    {
                        PropertyInfo propertiesProperty = typeDefinitionType.GetProperty("Properties");
                        var properties = propertiesProperty.GetValue(type) as System.Collections.IEnumerable;
                        
                        foreach (object property in properties)
                        {
                            if (ObfuscatePropertyName(property, propertyDefinitionType))
                            {
                                processedMembers++;
                            }
                        }
                    }
                }
                
                logger.Log($"Obfuscated {processedTypes} types and {processedMembers} members");
                
                // Write assembly to a temporary file first to avoid corruption
                string tempPath = assemblyPath + ".tmp";
                
                try
                {
                    // Write to temp file
                    MethodInfo writeMethod = assemblyDefinitionType.GetMethod("Write", 
                        BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                        
                    if (writeMethod == null)
                    {
                        logger.LogError("Could not find Write method");
                        return false;
                    }
                    
                    writeMethod.Invoke(assembly, new object[] { tempPath });
                    logger.Log($"Assembly written to temp file");
                    
                    // Dispose the assembly to release file locks
                    if (assembly is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    // Wait a bit for file handles to be released
                    System.Threading.Thread.Sleep(100);
                    
                    // Replace original file with temp file
                    if (File.Exists(assemblyPath))
                    {
                        File.Delete(assemblyPath);
                    }
                    File.Move(tempPath, assemblyPath);
                    
                    logger.Log($"Assembly written successfully: {Path.GetFileName(assemblyPath)}");
                }
                catch (Exception writeEx)
                {
                    logger.LogError($"Failed to write assembly: {writeEx.Message}");
                    logger.LogError($"Stack trace: {writeEx.StackTrace}");
                    
                    // Clean up temp file
                    if (File.Exists(tempPath))
                    {
                        try
                        {
                            File.Delete(tempPath);
                        }
                        catch { }
                    }
                    
                    // Make sure assembly is disposed
                    if (assembly is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return false;
            }
        }
        
        /// <summary>
        /// Get Mono.Cecil assembly
        /// </summary>
        private Assembly GetCecilAssembly()
        {
            try
            {
                // Try to load from GAC or already loaded
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Mono.Cecil");
                    
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }
                
                // Try to load from Libs folder
                string cecilPath = Path.Combine(
                    UnityEngine.Application.dataPath,
                    "UnityObfuscator/Libs/Mono.Cecil.dll"
                );
                
                if (File.Exists(cecilPath))
                {
                    return Assembly.LoadFrom(cecilPath);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load Mono.Cecil: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if a type should be excluded from obfuscation
        /// </summary>
        private bool ShouldExcludeType(string typeName)
        {
            // Check excluded namespaces
            foreach (var ns in config.excludedNamespaces)
            {
                if (typeName.StartsWith(ns + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            
            // Check excluded types
            if (config.excludedTypes.Contains(typeName))
            {
                return true;
            }
            
            // Exclude compiler-generated types
            if (typeName.Contains("<") || typeName.Contains(">"))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Generate an obfuscated name
        /// </summary>
        private string GenerateObfuscatedName()
        {
            // Use a simple naming scheme: a, b, c, ..., z, aa, ab, ...
            int n = nameCounter++;
            string name = "";
            
            do
            {
                name = (char)('a' + (n % 26)) + name;
                n /= 26;
            } while (n > 0);
            
            return name;
        }
        
        /// <summary>
        /// Obfuscate type name
        /// </summary>
        private bool ObfuscateTypeName(object type, Type typeDefinitionType)
        {
            try
            {
                PropertyInfo nameProperty = typeDefinitionType.GetProperty("Name");
                string currentName = nameProperty.GetValue(type) as string;
                
                // Don't rename special types
                if (currentName.StartsWith("<") || currentName == "Program")
                {
                    return false;
                }
                
                // Check if this is a Unity serializable type
                bool isUnitySerializable = IsUnitySerializableType(type, typeDefinitionType);
                
                // If we want to preserve Unity types OR if we want to obfuscate but patch assets
                if (isUnitySerializable)
                {
                    if (config.preserveMonoBehaviourNames)
                    {
                        // Traditional approach: preserve the name
                        logger.Log($"Preserving Unity serializable type: {currentName}");
                        return false;
                    }
                    else
                    {
                        // New approach: obfuscate and record mapping for asset patching
                        logger.Log($"Will obfuscate Unity serializable type (will patch assets): {currentName}");
                    }
                }
                
                // Don't rename types with serialization attributes if preserving
                if (config.preserveMonoBehaviourNames && HasSerializationAttribute(type, typeDefinitionType))
                {
                    logger.Log($"Preserving type with serialization attribute: {currentName}");
                    return false;
                }
                
                string newName = GenerateObfuscatedName();
                nameProperty.SetValue(type, newName);
                nameMap[currentName] = newName;
                
                // Register mapping for asset patching
                assetPatcher.RegisterTypeMapping(currentName, newName);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Obfuscate method name
        /// </summary>
        private bool ObfuscateMethodName(object method, Type methodDefinitionType)
        {
            try
            {
                PropertyInfo nameProperty = methodDefinitionType.GetProperty("Name");
                string currentName = nameProperty.GetValue(method) as string;
                
                // Don't rename special methods
                if (currentName.StartsWith(".") || currentName.StartsWith("get_") || 
                    currentName.StartsWith("set_") || currentName.StartsWith("add_") ||
                    currentName.StartsWith("remove_") || currentName == "Main")
                {
                    return false;
                }
                
                // Check for Unity special methods
                if (IsUnitySpecialMethod(currentName))
                {
                    return false;
                }
                
                string newName = GenerateObfuscatedName();
                nameProperty.SetValue(method, newName);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Obfuscate field name
        /// </summary>
        private bool ObfuscateFieldName(object field, Type fieldDefinitionType)
        {
            try
            {
                PropertyInfo nameProperty = fieldDefinitionType.GetProperty("Name");
                string currentName = nameProperty.GetValue(field) as string;
                
                // Don't rename backing fields
                if (currentName.StartsWith("<") || currentName.StartsWith("_"))
                {
                    return false;
                }
                
                // Check if field has preserve attributes (SerializeField, etc.)
                bool hasPreserveAttribute = HasPreserveAttribute(field, fieldDefinitionType);
                
                if (hasPreserveAttribute)
                {
                    if (config.preserveSerializedFields)
                    {
                        // Traditional approach: preserve the name
                        logger.Log($"Preserving field with attribute: {currentName}");
                        return false;
                    }
                    else
                    {
                        // New approach: obfuscate and record mapping for asset patching
                        logger.Log($"Will obfuscate serialized field (will patch assets): {currentName}");
                    }
                }
                
                string newName = GenerateObfuscatedName();
                nameProperty.SetValue(field, newName);
                
                // Register field mapping for asset patching (if this field is serialized)
                if (hasPreserveAttribute && !string.IsNullOrEmpty(currentTypeName))
                {
                    assetPatcher.RegisterFieldMapping(currentTypeName, currentName, newName);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Obfuscate property name
        /// </summary>
        private bool ObfuscatePropertyName(object property, Type propertyDefinitionType)
        {
            try
            {
                PropertyInfo nameProperty = propertyDefinitionType.GetProperty("Name");
                string currentName = nameProperty.GetValue(property) as string;
                
                string newName = GenerateObfuscatedName();
                nameProperty.SetValue(property, newName);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Check if method is a Unity special method
        /// </summary>
        private bool IsUnitySpecialMethod(string methodName)
        {
            string[] specialMethods = new[]
            {
                "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
                "OnEnable", "OnDisable", "OnDestroy", "OnApplicationQuit",
                "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
                "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
                "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseExit",
                "OnCollisionEnter2D", "OnCollisionExit2D", "OnCollisionStay2D",
                "OnTriggerEnter2D", "OnTriggerExit2D", "OnTriggerStay2D",
                "OnBecameVisible", "OnBecameInvisible", "OnGUI", "OnDrawGizmos",
                "OnDrawGizmosSelected", "OnValidate", "Reset"
            };
            
            return specialMethods.Contains(methodName);
        }
        
        /// <summary>
        /// Check if a type inherits from MonoBehaviour or ScriptableObject
        /// These types are referenced by Unity scenes and prefabs
        /// </summary>
        private bool IsUnitySerializableType(object type, Type typeDefinitionType)
        {
            try
            {
                // Get BaseType property
                PropertyInfo baseTypeProperty = typeDefinitionType.GetProperty("BaseType");
                if (baseTypeProperty == null) return false;
                
                object currentType = type;
                
                // Walk up the inheritance chain
                for (int i = 0; i < 20 && currentType != null; i++) // Limit depth to prevent infinite loops
                {
                    PropertyInfo nameProperty = typeDefinitionType.GetProperty("Name");
                    string typeName = nameProperty?.GetValue(currentType) as string;
                    
                    // Check if this is a Unity serializable base type
                    if (typeName == "MonoBehaviour" || typeName == "ScriptableObject" || 
                        typeName == "StateMachineBehaviour" || typeName == "Editor")
                    {
                        return true;
                    }
                    
                    // Get namespace
                    PropertyInfo namespaceProperty = typeDefinitionType.GetProperty("Namespace");
                    string typeNamespace = namespaceProperty?.GetValue(currentType) as string;
                    
                    // If we're in UnityEngine or UnityEditor namespace, it's a Unity type
                    if (!string.IsNullOrEmpty(typeNamespace) && 
                        (typeNamespace.StartsWith("UnityEngine") || typeNamespace.StartsWith("UnityEditor")))
                    {
                        return true;
                    }
                    
                    // Move to base type
                    currentType = baseTypeProperty.GetValue(currentType);
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Check if a type has serialization attributes ([Serializable])
        /// </summary>
        private bool HasSerializationAttribute(object type, Type typeDefinitionType)
        {
            try
            {
                PropertyInfo hasCustomAttributesProperty = typeDefinitionType.GetProperty("HasCustomAttributes");
                if (hasCustomAttributesProperty == null) return false;
                
                bool hasAttributes = (bool)hasCustomAttributesProperty.GetValue(type);
                if (!hasAttributes) return false;
                
                PropertyInfo customAttributesProperty = typeDefinitionType.GetProperty("CustomAttributes");
                if (customAttributesProperty == null) return false;
                
                var attributes = customAttributesProperty.GetValue(type) as System.Collections.IEnumerable;
                if (attributes == null) return false;
                
                foreach (var attr in attributes)
                {
                    Type customAttributeType = attr.GetType();
                    PropertyInfo attributeTypeProperty = customAttributeType.GetProperty("AttributeType");
                    if (attributeTypeProperty == null) continue;
                    
                    object attributeType = attributeTypeProperty.GetValue(attr);
                    if (attributeType == null) continue;
                    
                    PropertyInfo fullNameProperty = attributeType.GetType().GetProperty("FullName");
                    string attributeFullName = fullNameProperty?.GetValue(attributeType) as string;
                    
                    if (!string.IsNullOrEmpty(attributeFullName) && 
                        (attributeFullName.Contains("Serializable") || attributeFullName.Contains("Preserve")))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Check if a field/property has preserve attributes (SerializeField, etc.)
        /// </summary>
        private bool HasPreserveAttribute(object member, Type memberDefinitionType)
        {
            try
            {
                PropertyInfo hasCustomAttributesProperty = memberDefinitionType.GetProperty("HasCustomAttributes");
                if (hasCustomAttributesProperty == null) return false;
                
                bool hasAttributes = (bool)hasCustomAttributesProperty.GetValue(member);
                if (!hasAttributes) return false;
                
                PropertyInfo customAttributesProperty = memberDefinitionType.GetProperty("CustomAttributes");
                if (customAttributesProperty == null) return false;
                
                var attributes = customAttributesProperty.GetValue(member) as System.Collections.IEnumerable;
                if (attributes == null) return false;
                
                foreach (var attr in attributes)
                {
                    Type customAttributeType = attr.GetType();
                    PropertyInfo attributeTypeProperty = customAttributeType.GetProperty("AttributeType");
                    if (attributeTypeProperty == null) continue;
                    
                    object attributeType = attributeTypeProperty.GetValue(attr);
                    if (attributeType == null) continue;
                    
                    PropertyInfo fullNameProperty = attributeType.GetType().GetProperty("FullName");
                    string attributeFullName = fullNameProperty?.GetValue(attributeType) as string;
                    
                    if (string.IsNullOrEmpty(attributeFullName)) continue;
                    
                    // Check against configured preserve attributes
                    foreach (var preserveAttr in config.preserveAttributes)
                    {
                        if (attributeFullName == preserveAttr || attributeFullName.EndsWith("." + preserveAttr))
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
