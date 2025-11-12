using UnityEditor;
using UnityEngine;
using System.IO;

namespace UnityObfuscator.Editor
{
    /// <summary>
    /// Editor window for Unity Obfuscator plugin
    /// </summary>
    public class ObfuscatorWindow : EditorWindow
    {
        private const string CONFIG_FILE = "Assets/UnityObfuscator/Config/obfuscator_config.json";
        
        private ObfuscatorConfig config;
        private Vector2 scrollPosition;
        private bool showGeneralSettings = true;
        private bool showTargetSettings = true;
        private bool showObfuscationOptions = true;
        private bool showExclusions = true;
        private bool showAdvanced = false;
        
        [MenuItem("Tools/Unity Obfuscator Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<ObfuscatorWindow>("Unity Obfuscator");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadConfiguration();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Header
            GUILayout.Space(10);
            GUILayout.Label("Unity Obfuscator", EditorStyles.boldLabel);
            GUILayout.Label("IL2CPP Assembly Obfuscation Plugin", EditorStyles.miniLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This plugin obfuscates Unity assemblies before IL2CPP conversion. " +
                "Configure the settings below and click 'Run Obfuscation' or enable 'Auto Run on Build'.",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // General Settings
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
            if (showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                config.enableObfuscation = EditorGUILayout.Toggle("Enable Obfuscation", config.enableObfuscation);
                config.autoRunOnBuild = EditorGUILayout.Toggle("Auto Run on Build", config.autoRunOnBuild);
                config.enableBackup = EditorGUILayout.Toggle("Enable Backup", config.enableBackup);
                config.backupPath = EditorGUILayout.TextField("Backup Path", config.backupPath);
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
            
            // Target Assemblies
            showTargetSettings = EditorGUILayout.Foldout(showTargetSettings, "Target Assemblies", true);
            if (showTargetSettings)
            {
                EditorGUI.indentLevel++;
                config.assemblyPath = EditorGUILayout.TextField("Assembly Path", config.assemblyPath);
                
                GUILayout.Label("Target Assemblies:");
                for (int i = 0; i < config.targetAssemblies.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    config.targetAssemblies[i] = EditorGUILayout.TextField(config.targetAssemblies[i]);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        config.targetAssemblies.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("Add Assembly"))
                {
                    config.targetAssemblies.Add("Assembly-CSharp.dll");
                }
                
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
            
            // Obfuscation Options
            showObfuscationOptions = EditorGUILayout.Foldout(showObfuscationOptions, "Obfuscation Options", true);
            if (showObfuscationOptions)
            {
                EditorGUI.indentLevel++;
                config.obfuscateTypeNames = EditorGUILayout.Toggle("Obfuscate Type Names", config.obfuscateTypeNames);
                config.obfuscateMethodNames = EditorGUILayout.Toggle("Obfuscate Method Names", config.obfuscateMethodNames);
                config.obfuscateFieldNames = EditorGUILayout.Toggle("Obfuscate Field Names", config.obfuscateFieldNames);
                config.obfuscatePropertyNames = EditorGUILayout.Toggle("Obfuscate Property Names", config.obfuscatePropertyNames);
                config.encryptStrings = EditorGUILayout.Toggle("Encrypt Strings", config.encryptStrings);
                
                if (config.encryptStrings)
                {
                    EditorGUI.indentLevel++;
                    config.autoGenerateKey = EditorGUILayout.Toggle("Auto Generate Key", config.autoGenerateKey);
                    if (!config.autoGenerateKey)
                    {
                        config.encryptionKey = EditorGUILayout.TextField("Encryption Key", config.encryptionKey);
                    }
                    EditorGUI.indentLevel--;
                }
                
                config.controlFlowObfuscation = EditorGUILayout.Toggle("Control Flow Obfuscation", config.controlFlowObfuscation);
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
            
            // Exclusions
            showExclusions = EditorGUILayout.Foldout(showExclusions, "Exclusions", true);
            if (showExclusions)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Label("Excluded Namespaces:");
                for (int i = 0; i < config.excludedNamespaces.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    config.excludedNamespaces[i] = EditorGUILayout.TextField(config.excludedNamespaces[i]);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        config.excludedNamespaces.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Add Namespace"))
                {
                    config.excludedNamespaces.Add("MyNamespace");
                }
                
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
            
            // Advanced Settings
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings", true);
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                config.verboseLogging = EditorGUILayout.Toggle("Verbose Logging", config.verboseLogging);
                config.generateReport = EditorGUILayout.Toggle("Generate Report", config.generateReport);
                config.reportPath = EditorGUILayout.TextField("Report Path", config.reportPath);
                
                GUILayout.Space(5);
                GUILayout.Label("External Obfuscator Integration", EditorStyles.boldLabel);
                config.useExternalObfuscator = EditorGUILayout.Toggle("Use External Obfuscator", config.useExternalObfuscator);
                
                if (config.useExternalObfuscator)
                {
                    EditorGUI.indentLevel++;
                    config.externalObfuscatorPath = EditorGUILayout.TextField("Obfuscator Path", config.externalObfuscatorPath);
                    config.externalObfuscatorArgs = EditorGUILayout.TextField("Arguments", config.externalObfuscatorArgs);
                    EditorGUILayout.HelpBox("Use {assembly} as placeholder for assembly path", MessageType.Info);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
            
            GUILayout.Space(20);
            
            // Action Buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Run Obfuscation", GUILayout.Height(30)))
            {
                RunObfuscation();
            }
            
            if (GUILayout.Button("Rollback", GUILayout.Height(30)))
            {
                RollbackObfuscation();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Configuration"))
            {
                SaveConfiguration();
            }
            
            if (GUILayout.Button("Load Configuration"))
            {
                LoadConfiguration();
            }
            
            if (GUILayout.Button("Reset to Default"))
            {
                ResetConfiguration();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Help
            if (GUILayout.Button("Open Documentation"))
            {
                Application.OpenURL("file://" + Path.Combine(Application.dataPath, "UnityObfuscator/README.md"));
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void RunObfuscation()
        {
            if (!EditorUtility.DisplayDialog(
                "Run Obfuscation",
                "This will obfuscate the configured assemblies. A backup will be created if enabled. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }
            
            SaveConfiguration();
            
            string logPath = Path.Combine(config.reportPath, $"manual_obfuscation_{System.DateTime.Now:yyyyMMdd_HHmmss}.log");
            var logger = new ObfuscatorLogger(config.verboseLogging, logPath);
            var runner = new ObfuscatorRunner(config, logger);
            
            bool success = runner.Run();
            logger.SaveToFile();
            
            if (success)
            {
                EditorUtility.DisplayDialog("Success", "Obfuscation completed successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failed", "Obfuscation failed. Check the log for details.", "OK");
            }
            
            AssetDatabase.Refresh();
        }
        
        private void RollbackObfuscation()
        {
            if (!EditorUtility.DisplayDialog(
                "Rollback Obfuscation",
                "This will restore assemblies from the most recent backup. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }
            
            var logger = new ObfuscatorLogger(true);
            var runner = new ObfuscatorRunner(config, logger);
            
            bool success = runner.Rollback();
            
            if (success)
            {
                EditorUtility.DisplayDialog("Success", "Rollback completed successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failed", "Rollback failed. Check the console for details.", "OK");
            }
            
            AssetDatabase.Refresh();
        }
        
        private void SaveConfiguration()
        {
            try
            {
                string directory = Path.GetDirectoryName(CONFIG_FILE);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string json = config.ToJson();
                File.WriteAllText(CONFIG_FILE, json);
                
                Debug.Log("[UnityObfuscator] Configuration saved to: " + CONFIG_FILE);
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save configuration: {ex.Message}", "OK");
            }
        }
        
        private void LoadConfiguration()
        {
            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    string json = File.ReadAllText(CONFIG_FILE);
                    config = ObfuscatorConfig.LoadFromJson(json);
                    Debug.Log("[UnityObfuscator] Configuration loaded from: " + CONFIG_FILE);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UnityObfuscator] Failed to load configuration: {ex.Message}");
                }
            }
            
            config = ObfuscatorConfig.CreateDefault();
        }
        
        private void ResetConfiguration()
        {
            if (EditorUtility.DisplayDialog(
                "Reset Configuration",
                "This will reset all settings to default values. Continue?",
                "Yes", "Cancel"))
            {
                config = ObfuscatorConfig.CreateDefault();
                SaveConfiguration();
            }
        }
    }
}
