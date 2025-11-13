# Unity Obfuscator Plugin

A comprehensive Unity Editor plugin for obfuscating IL2CPP assemblies before native code generation. This plugin provides transparent integration into your Unity build pipeline with support for automated obfuscation, backup/rollback, and CI/CD workflows.

## Features

- **Automated Build Integration**: Hooks into Unity's build pipeline (IPostBuildPlayerScriptDLLs)
- **Manual Control**: Editor window for configuration and manual execution
- **CLI Support**: Batch mode execution for CI/CD pipelines
- **Obfuscation Capabilities**:
  - Type, method, field, and property name obfuscation
  - String constant encryption (AES-256)
  - Optional control flow obfuscation (via external tools)
- **Safety Features**:
  - Automatic backup before obfuscation
  - One-click rollback functionality
  - Whitelist system for Unity framework classes
  - Attribute-based exclusion (e.g., `[Preserve]`, `[SerializeField]`)
- **Flexible Architecture**:
  - Built-in obfuscator using Mono.Cecil
  - External obfuscator integration support
  - Configurable via JSON
- **Comprehensive Logging**: Detailed reports and logs for debugging

## Installation

1. Copy the `UnityObfuscator` folder to your project's `Assets` directory
2. Download Mono.Cecil library (required for built-in obfuscation):
   - Visit: https://www.nuget.org/packages/Mono.Cecil/
   - Extract `Mono.Cecil.dll` to `Assets/UnityObfuscator/Libs/`
   - Alternatively, configure an external obfuscator

## Quick Start

### Using the Editor Window

1. Open **Tools > Unity Obfuscator** from the Unity menu
2. Configure your settings:
   - Enable obfuscation features you need
   - Add target assemblies (default: `Assembly-CSharp.dll`)
   - Configure exclusions and whitelists
3. Click **Run Obfuscation** to test manually
4. Enable **Auto Run on Build** for automatic obfuscation during builds

### Using CLI for CI/CD

```bash
# Run obfuscation
Unity.exe -quit -batchmode -projectPath "path/to/project" \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation

# With custom config
Unity.exe -quit -batchmode -projectPath "path/to/project" \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation \
  -configPath "path/to/custom_config.json"

# Rollback
Unity.exe -quit -batchmode -projectPath "path/to/project" \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.Rollback

# Generate default config
Unity.exe -quit -batchmode -projectPath "path/to/project" \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.GenerateConfig
```

## Configuration

Configuration is stored in `Assets/UnityObfuscator/Config/obfuscator_config.json`.

### Key Configuration Options

```json
{
  "enableObfuscation": true,           // Master switch
  "autoRunOnBuild": false,             // Run automatically during builds
  "enableBackup": true,                // Backup before obfuscation
  "assemblyPath": "Library/ScriptAssemblies",  // Where to find DLLs
  "targetAssemblies": [                // Which assemblies to obfuscate
    "Assembly-CSharp.dll"
  ],
  "obfuscateTypeNames": true,          // Rename types
  "obfuscateMethodNames": true,        // Rename methods
  "obfuscateFieldNames": true,         // Rename fields
  "encryptStrings": false,             // Encrypt string literals
  "excludedNamespaces": [              // Don't obfuscate these namespaces
    "UnityEngine",
    "UnityEditor"
  ]
}
```

### Exclusion Rules

The plugin automatically excludes:

- Unity framework classes (`UnityEngine.*`, `UnityEditor.*`)
- System libraries (`System.*`, `Mono.*`)
- Serialized fields (marked with `[SerializeField]`)
- Special Unity methods (Awake, Start, Update, etc.)
- Compiler-generated code

You can add custom exclusions:

```json
{
  "excludedNamespaces": ["MyNamespace.Editor"],
  "excludedTypes": ["MyNamespace.ImportantClass"],
  "preserveAttributes": [
    "UnityEngine.SerializeField",
    "MyCustom.PreserveAttribute"
  ]
}
```

## String Encryption

When `encryptStrings` is enabled:

1. The plugin generates an AES-256 encryption key
2. String literals in your code are encrypted
3. A runtime decryption helper is injected
4. Strings are decrypted on-demand at runtime

**Note**: String encryption requires Mono.Cecil and may impact runtime performance slightly.

## External Obfuscator Integration

You can use commercial obfuscators instead of the built-in one:

```json
{
  "useExternalObfuscator": true,
  "externalObfuscatorPath": "C:/Tools/Obfuscator/obfuscator.exe",
  "externalObfuscatorArgs": "-input {assembly} -output {assembly}"
}
```

Use `{assembly}` as a placeholder for the assembly path.

## Backup and Rollback

### Automatic Backups

When `enableBackup` is true, the plugin:
- Creates timestamped backups in `Library/ObfuscatorBackups/`
- Keeps the 5 most recent backups
- Includes DLL, PDB, and MDB files

### Manual Rollback

1. Open **Tools > Unity Obfuscator**
2. Click **Rollback**
3. Assemblies are restored from the most recent backup

### CLI Rollback

```bash
Unity.exe -quit -batchmode -projectPath "path/to/project" \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.Rollback
```

## Build Pipeline Integration

The plugin hooks into Unity's build pipeline using `IPostBuildPlayerScriptDLLs`:

```
C# Compilation → Copy to Staging Area → [Obfuscation] → IL2CPP → Native Binary
```

This ensures IL2CPP converts the obfuscated assemblies, not the original ones. The obfuscation happens after Unity copies script DLLs to the build staging area (`Temp/StagingArea/`) but before IL2CPP conversion.

To enable automatic obfuscation during builds:
- Set `autoRunOnBuild: true` in the configuration
- Or check "Auto Run on Build" in the Editor window

If obfuscation fails during a build, you'll be prompted to continue or cancel.

## Logging and Reports

### Log Files

Logs are saved to `Library/ObfuscatorReports/`:
- `build_obfuscation_<timestamp>.log` - Build-triggered obfuscation
- `manual_obfuscation_<timestamp>.log` - Manual execution
- `cli_obfuscation_<timestamp>.log` - CLI execution

### Reports

When `generateReport` is enabled, detailed reports include:
- Configuration used
- List of processed assemblies
- Statistics (types/methods/fields obfuscated)
- Full operation log

## Troubleshooting

### "Mono.Cecil library not found"

**Solution**: Download Mono.Cecil from NuGet and place `Mono.Cecil.dll` in `Assets/UnityObfuscator/Libs/`

```bash
# Download and extract Mono.Cecil
wget https://www.nuget.org/api/v2/package/Mono.Cecil/0.11.5 -O cecil.zip
unzip cecil.zip
cp lib/netstandard2.0/Mono.Cecil.dll Assets/UnityObfuscator/Libs/
```

### "Assembly not found" during obfuscation

**Cause**: Assemblies don't exist yet (usually when running before a build)

**Solution**: 
- Run obfuscation during/after a build
- Or compile scripts first: Assets > Reimport All

### Serialization issues after obfuscation

**Cause**: Serialized fields were obfuscated

**Solution**: Ensure `UnityEngine.SerializeField` is in `preserveAttributes`

### Reflection not working

**Cause**: Type/member names were obfuscated

**Solution**: Add affected types/namespaces to exclusion lists

### Build errors after obfuscation

**Solution**: Use the Rollback feature to restore original assemblies

### IL2CPP compilation fails

**Cause**: Invalid IL after obfuscation

**Solution**: 
- Disable specific obfuscation features (try disabling name obfuscation first)
- Add problematic types to exclusions
- Use rollback and investigate logs

## Best Practices

1. **Test First**: Run manual obfuscation before enabling auto-run
2. **Start Small**: Begin with just name obfuscation, add features incrementally
3. **Exclude Liberally**: When in doubt, exclude it
4. **Keep Backups**: Don't disable the backup feature
5. **Version Control**: Don't commit `Library/` folder (obfuscated assemblies)
6. **CI Integration**: Use separate config files for different environments
7. **Monitor Logs**: Check reports after obfuscation to verify success

## Example CI Workflow

```yaml
# GitHub Actions example
- name: Obfuscate Unity Assemblies
  run: |
    Unity.exe -quit -batchmode \
      -projectPath ${{ github.workspace }} \
      -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation \
      -logFile obfuscation.log
      
- name: Build IL2CPP
  run: |
    Unity.exe -quit -batchmode \
      -projectPath ${{ github.workspace }} \
      -buildTarget Android \
      -executeMethod BuildScript.Build
```

## Architecture

```
Assets/UnityObfuscator/
├── Editor/
│   ├── ObfuscatorConfig.cs        # Configuration data model
│   ├── ObfuscatorRunner.cs        # Main orchestration
│   ├── CecilObfuscator.cs         # Built-in obfuscator
│   ├── BackupManager.cs           # Backup/rollback logic
│   ├── ObfuscatorLogger.cs        # Logging utility
│   ├── ObfuscatorWindow.cs        # Editor UI
│   ├── ObfuscatorBuildHook.cs     # Build pipeline integration
│   ├── ObfuscatorCLI.cs           # Command-line interface
│   └── StringEncryption.cs        # AES encryption utilities
├── Runtime/
│   └── ObfuscatedString.cs        # Runtime string decryption
├── Libs/
│   └── Mono.Cecil.dll             # (Download separately)
├── Config/
│   └── obfuscator_config.json     # Default configuration
└── README.md                       # This file
```

## License

This plugin is provided as-is for educational and commercial use.

## Support

For issues, questions, or contributions:
- Check the troubleshooting section
- Review generated logs in `Library/ObfuscatorReports/`
- Open an issue on the repository

## Comparison with Unity Obfuscation Pro

This plugin provides similar functionality to commercial solutions:

| Feature | Unity Obfuscator | Commercial Tools |
|---------|------------------|------------------|
| Name Obfuscation | ✅ | ✅ |
| String Encryption | ✅ | ✅ |
| Control Flow | 🔧 (via external) | ✅ |
| IL2CPP Compatible | ✅ | ✅ |
| Editor Integration | ✅ | ✅ |
| CLI Support | ✅ | ✅ |
| Backup/Rollback | ✅ | ⚠️ |
| Open Source | ✅ | ❌ |
| Price | Free | $$$ |

## Changelog

### Version 1.0.0
- Initial release
- Name obfuscation (types, methods, fields, properties)
- String encryption support
- Build pipeline integration
- Editor window
- CLI support
- Backup and rollback
- External obfuscator integration

## Roadmap

- [ ] Enhanced control flow obfuscation (built-in)
- [ ] Resource name obfuscation
- [ ] Anti-tampering checks
- [ ] Symbol stripping
- [ ] Incremental obfuscation
- [ ] Unity Package Manager distribution
