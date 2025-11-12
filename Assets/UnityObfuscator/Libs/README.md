# Mono.Cecil Library

This folder should contain the Mono.Cecil.dll library required for the built-in obfuscator.

## Installation

### Option 1: Download from NuGet (Recommended)

1. Visit: https://www.nuget.org/packages/Mono.Cecil/0.11.5
2. Download the package
3. Extract the `.nupkg` file (it's a ZIP archive)
4. Copy `lib/netstandard2.0/Mono.Cecil.dll` to this folder
5. Restart Unity Editor

### Option 2: Command Line

```bash
# Download and extract
curl -L -o cecil.zip https://www.nuget.org/api/v2/package/Mono.Cecil/0.11.5
unzip cecil.zip
cp lib/netstandard2.0/Mono.Cecil.dll Assets/UnityObfuscator/Libs/
rm -rf cecil.zip lib _rels package *.nuspec '[Content_Types].xml'
```

### Option 3: Manual Download

1. Go to: https://github.com/jbevain/cecil/releases
2. Download the latest release
3. Extract and copy `Mono.Cecil.dll` to this folder

## Required Files

- ✅ **Mono.Cecil.dll** (Required)
- ⚠️ Mono.Cecil.Pdb.dll (Optional, for PDB support)
- ⚠️ Mono.Cecil.Mdb.dll (Optional, for MDB support)

## Alternative: Use External Obfuscator

If you don't want to use Mono.Cecil, you can configure the plugin to use an external obfuscator:

```json
{
  "useExternalObfuscator": true,
  "externalObfuscatorPath": "C:/Tools/Obfuscator/obfuscator.exe",
  "externalObfuscatorArgs": "-input {assembly} -output {assembly}"
}
```

## Troubleshooting

### "Mono.Cecil library not found"

This error means the DLL is not in this folder or Unity hasn't loaded it yet.

**Solution:**
1. Verify `Mono.Cecil.dll` exists in this folder
2. Restart Unity Editor
3. Check that the file is not corrupted (should be ~300KB)

### "Could not load file or assembly 'Mono.Cecil'"

This means there's a version mismatch or the DLL is corrupted.

**Solution:**
1. Delete all DLLs in this folder
2. Download Mono.Cecil 0.11.5 specifically
3. Use the netstandard2.0 version (not net40 or net35)

## Version Information

- **Recommended Version**: Mono.Cecil 0.11.5
- **Minimum Version**: Mono.Cecil 0.10.0
- **Target Framework**: .NET Standard 2.0

## License

Mono.Cecil is licensed under the MIT License.
See: https://github.com/jbevain/cecil/blob/master/LICENSE.txt
