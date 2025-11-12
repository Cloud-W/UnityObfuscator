# Unity Obfuscator - Troubleshooting Guide

This guide covers common issues and their solutions when using the Unity Obfuscator plugin.

## Table of Contents

1. [Installation Issues](#installation-issues)
2. [Configuration Issues](#configuration-issues)
3. [Obfuscation Failures](#obfuscation-failures)
4. [Runtime Issues](#runtime-issues)
5. [Build Issues](#build-issues)
6. [Performance Issues](#performance-issues)

---

## Installation Issues

### Issue: "Mono.Cecil library not found"

**Symptoms**:
```
[UnityObfuscator] ERROR: Mono.Cecil library not found
Could not find Mono.Cecil.AssemblyDefinition type
```

**Solution**:
1. Download Mono.Cecil from NuGet: https://www.nuget.org/packages/Mono.Cecil/
2. Extract the `.nupkg` file (it's just a ZIP)
3. Copy `lib/netstandard2.0/Mono.Cecil.dll` to `Assets/UnityObfuscator/Libs/`
4. Restart Unity Editor

**Alternative**: Use an external obfuscator instead:
```json
{
  "useExternalObfuscator": true,
  "externalObfuscatorPath": "path/to/obfuscator.exe"
}
```

### Issue: Editor window not appearing in menu

**Symptoms**: Can't find "Tools > Unity Obfuscator" menu

**Causes**:
- Script compilation errors
- Missing files
- Unity version incompatibility

**Solution**:
1. Check Console for compilation errors
2. Ensure all files are in correct locations
3. Try reimporting: Right-click `UnityObfuscator` folder > Reimport
4. Restart Unity Editor

---

## Configuration Issues

### Issue: Configuration not saving

**Symptoms**: Settings reset after closing Editor window

**Solution**:
1. Click "Save Configuration" button in the Editor window
2. Verify `Assets/UnityObfuscator/Config/obfuscator_config.json` exists
3. Check file permissions (should be writable)
4. Check for JSON syntax errors in the config file

### Issue: "Configuration validation failed"

**Symptoms**: Error when running obfuscation

**Common causes and solutions**:

1. **No target assemblies specified**
   ```json
   "targetAssemblies": ["Assembly-CSharp.dll"]
   ```

2. **String encryption enabled without key**
   ```json
   "encryptStrings": true,
   "autoGenerateKey": true
   ```

3. **External obfuscator path invalid**
   ```json
   "useExternalObfuscator": false
   ```
   Or provide valid path

---

## Obfuscation Failures

### Issue: "Assembly not found"

**Symptoms**:
```
[UnityObfuscator] WARNING: Target assembly not found: Library/ScriptAssemblies/Assembly-CSharp.dll
```

**Causes**:
- Assemblies haven't been compiled yet
- Wrong assembly path
- Unity version differences

**Solutions**:

1. **Compile scripts first**:
   - Menu: Assets > Reimport All
   - Or trigger a build first

2. **Verify assembly path**:
   - Unity 2019-2021: `Library/ScriptAssemblies`
   - Check actual location in your project

3. **Wait for compilation**:
   - Editor might still be compiling
   - Wait for spinner to stop

### Issue: Obfuscation succeeds but no changes visible

**Symptoms**: Obfuscation reports success but code looks unchanged

**Explanation**: You won't see changes in your source `.cs` files. Obfuscation modifies compiled `.dll` files in `Library/ScriptAssemblies/`.

**Verification**:
1. Check the obfuscation report in `Library/ObfuscatorReports/`
2. Look for "Obfuscated X types and Y members"
3. Decompile the DLL with a tool like ILSpy to verify

### Issue: "Failed to read assembly"

**Symptoms**: Cecil can't read the assembly

**Causes**:
- Corrupted DLL
- DLL is locked by another process
- Incompatible DLL format

**Solutions**:
1. Clean and rebuild: Edit > Preferences > Clear Cache
2. Close Unity and delete `Library/` folder, reopen
3. Check if antivirus is locking files
4. Try with a fresh Unity project

### Issue: Obfuscation hangs or takes very long

**Symptoms**: Process doesn't complete

**Solutions**:
1. Disable verbose logging:
   ```json
   "verboseLogging": false
   ```

2. Start with fewer features:
   ```json
   "obfuscateTypeNames": true,
   "obfuscateMethodNames": false,
   "obfuscateFieldNames": false,
   "encryptStrings": false
   ```

3. Exclude more namespaces:
   ```json
   "excludedNamespaces": ["UnityEngine", "UnityEditor", "System", "MyLargeNamespace"]
   ```

---

## Runtime Issues

### Issue: Serialization broken after obfuscation

**Symptoms**:
- Inspector shows missing references
- Saved data doesn't load
- Scene objects lose connections

**Cause**: Serialized field names were obfuscated

**Solution**: Preserve serialized members
```json
{
  "preserveAttributes": [
    "UnityEngine.SerializeField",
    "UnityEngine.Serializable"
  ],
  "obfuscateFieldNames": false
}
```

Or exclude specific types:
```json
{
  "excludedTypes": ["MyNamespace.SaveData", "MyNamespace.GameConfig"]
}
```

### Issue: Reflection not working

**Symptoms**:
```csharp
// Fails at runtime
Type.GetType("MyNamespace.MyClass"); // Returns null
typeof(MyClass).GetMethod("MyMethod"); // Returns null
```

**Cause**: Reflected type/member names were obfuscated

**Solutions**:

1. **Exclude reflected types**:
   ```json
   {
     "excludedNamespaces": ["MyNamespace.Plugins"]
   }
   ```

2. **Use typeof instead of strings**:
   ```csharp
   // Don't use
   Type.GetType("MyClass");
   
   // Use
   typeof(MyClass);
   ```

3. **Store type references before obfuscation**:
   ```csharp
   public class TypeCache
   {
       public static readonly Type MyClassType = typeof(MyClass);
   }
   ```

### Issue: "ObfuscatedString not initialized"

**Symptoms**: Runtime exception when accessing encrypted strings

**Cause**: String decryption key not set

**Solution**: Initialize at startup
```csharp
using UnityObfuscator.Runtime;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void InitializeObfuscator()
{
    ObfuscatedString.Initialize("YOUR_KEY_HERE");
}
```

The key is in the obfuscation report or config file.

### Issue: Unity callbacks not called

**Symptoms**: Start(), Update(), etc. don't execute

**Cause**: Method names were obfuscated

**Solution**: Unity callbacks are already excluded by default. If this happens:

1. Check your configuration:
   ```json
   "obfuscateMethodNames": true
   ```

2. Verify CecilObfuscator.IsUnitySpecialMethod() includes your methods

3. Use external obfuscator with better Unity support

---

## Build Issues

### Issue: IL2CPP build fails after obfuscation

**Symptoms**:
```
IL2CPP error: Failed to compile...
```

**Causes**:
- Invalid IL after obfuscation
- Unsupported IL instructions
- Broken type references

**Solutions**:

1. **Rollback immediately**:
   ```bash
   Unity.exe -batchmode -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.Rollback
   ```

2. **Disable obfuscation features incrementally**:
   ```json
   {
     "obfuscateTypeNames": false,  // Try this first
     "obfuscateMethodNames": true,
     "obfuscateFieldNames": true
   }
   ```

3. **Exclude problematic assemblies**:
   ```json
   {
     "targetAssemblies": ["Assembly-CSharp.dll"],
     "excludedAssemblies": ["Assembly-CSharp-firstpass"]
   }
   ```

4. **Check IL2CPP logs**: `Temp/StagingArea/Il2CppOutputProject/IL2CPP_Build_Log.txt`

### Issue: Build crashes with obfuscation enabled

**Symptoms**: Unity crashes during build

**Solutions**:
1. Disable auto-run and obfuscate manually first
2. Use external obfuscator instead
3. Update Unity to latest patch version
4. Reduce obfuscation scope

### Issue: "Build cancelled due to obfuscation failure"

**Symptoms**: Dialog appears during build

**Cause**: Obfuscation failed and you clicked "Cancel Build"

**Solutions**:
1. Check logs in `Library/ObfuscatorReports/`
2. Fix the issue (see error message)
3. Click "Continue" to build without obfuscation
4. Or disable auto-run temporarily:
   ```json
   "autoRunOnBuild": false
   ```

---

## Performance Issues

### Issue: Obfuscation takes too long

**Symptoms**: Minutes to complete

**Solutions**:

1. **Disable verbose logging**:
   ```json
   "verboseLogging": false
   ```

2. **Reduce target assemblies**:
   ```json
   "targetAssemblies": ["Assembly-CSharp.dll"]
   // Don't include first-pass or plugins unless necessary
   ```

3. **Exclude large namespaces**:
   ```json
   "excludedNamespaces": ["ThirdParty", "UnityEngine", "UnityEditor"]
   ```

4. **Disable string encryption**:
   ```json
   "encryptStrings": false
   ```

5. **Use external obfuscator** (often faster)

### Issue: Game runs slower after obfuscation

**Symptoms**: FPS drop, stuttering

**Likely causes**:

1. **String encryption overhead**:
   - Each encrypted string requires decryption
   - Disable if not critical:
     ```json
     "encryptStrings": false
     ```

2. **IL2CPP optimization affected**:
   - Obfuscated names might prevent some optimizations
   - Test with and without obfuscation
   - Use Unity Profiler to identify hotspots

**Mitigation**:
- Only obfuscate critical assemblies
- Exclude performance-critical code
- Profile before and after

---

## CI/CD Issues

### Issue: CLI obfuscation fails in CI

**Symptoms**: Exit code 1 from Unity in CI

**Solutions**:

1. **Check CI logs**: Unity batch mode logs everything to console

2. **Verify config file exists**:
   ```bash
   ls -la Assets/UnityObfuscator/Config/obfuscator_config.json
   ```

3. **Use explicit config path**:
   ```bash
   -configPath "Assets/UnityObfuscator/Config/ci_config.json"
   ```

4. **Test locally first**:
   ```bash
   Unity.exe -quit -batchmode -projectPath . \
     -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation \
     -logFile obfuscation.log
   ```

5. **Check Mono.Cecil availability in CI**:
   - Commit `Mono.Cecil.dll` to repository
   - Or download in CI before build

### Issue: Different behavior in CI vs local

**Cause**: Different configurations or Unity versions

**Solution**:
1. Use version-controlled config files
2. Ensure same Unity version
3. Commit all dependencies

---

## General Debugging Tips

### Enable Verbose Logging

```json
{
  "verboseLogging": true,
  "generateReport": true
}
```

Check: `Library/ObfuscatorReports/`

### Test Step-by-Step

1. Start with all obfuscation disabled
2. Enable one feature at a time
3. Build and test after each change
4. Identify which feature causes issues

### Check Reports

Reports show exactly what was obfuscated:
```
Library/ObfuscatorReports/obfuscation_report_20250112_143000.txt
```

### Use Rollback

Don't struggle with broken builds:
```
Tools > Unity Obfuscator > Rollback
```

### Test in Isolated Project

Create a minimal test project to isolate the issue.

---

## Getting Help

If you're still stuck:

1. **Check logs**: `Library/ObfuscatorReports/`
2. **Check Unity Console**: For compilation errors
3. **Try with default config**: Rule out configuration issues
4. **Test with minimal scene**: Rule out project-specific issues
5. **Check Unity version**: Some versions have known issues
6. **Open an issue**: Provide logs and configuration

---

## Known Limitations

1. **Generic constraints**: May not be preserved correctly
2. **Async/await**: Some edge cases not fully supported
3. **Unity serialization**: Can conflict with name obfuscation
4. **Reflection-heavy code**: Requires extensive exclusions
5. **Hot reload**: Doesn't work with obfuscated assemblies

## Best Practice Checklist

- [ ] Start with minimal obfuscation
- [ ] Test thoroughly before enabling auto-run
- [ ] Always keep backups enabled
- [ ] Exclude Unity framework classes
- [ ] Exclude reflection-used types
- [ ] Preserve serialized fields
- [ ] Test IL2CPP builds
- [ ] Monitor performance impact
- [ ] Version control your config
- [ ] Document custom exclusions

---

*Last updated: 2025-01-12*
