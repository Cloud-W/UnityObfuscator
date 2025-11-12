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
        
        public CecilObfuscator(ObfuscatorConfig config, ObfuscatorLogger logger)
        {
            this.config = config;
            this.logger = logger;
            this.nameMap = new Dictionary<string, string>();
            this.nameCounter = 0;
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
                
                if (assemblyDefinitionType == null)
                {
                    logger.LogError("Could not find Mono.Cecil.AssemblyDefinition type");
                    return false;
                }
                
                // Read assembly
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
                
                // Write assembly back
                MethodInfo writeMethod = assemblyDefinitionType.GetMethod("Write", 
                    BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    
                if (writeMethod != null)
                {
                    writeMethod.Invoke(assembly, new object[] { assemblyPath });
                    logger.Log($"Assembly written successfully: {Path.GetFileName(assemblyPath)}");
                }
                
                // Dispose assembly
                if (assembly is IDisposable disposable)
                {
                    disposable.Dispose();
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
                if (currentName.StartsWith("<") || currentName == "Program" || currentName.Contains("MonoBehaviour"))
                {
                    return false;
                }
                
                string newName = GenerateObfuscatedName();
                nameProperty.SetValue(type, newName);
                nameMap[currentName] = newName;
                
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
                
                string newName = GenerateObfuscatedName();
                nameProperty.SetValue(field, newName);
                
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
                "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseExit"
            };
            
            return specialMethods.Contains(methodName);
        }
    }
}
