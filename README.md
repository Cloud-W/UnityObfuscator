# Unity Obfuscator

A comprehensive, open-source Unity Editor plugin for obfuscating IL2CPP assemblies. Protect your game code from decompilation and reverse engineering without modifying source files.

## 🎯 Overview

Unity Obfuscator integrates seamlessly into your Unity build pipeline, automatically obfuscating managed assemblies before IL2CPP conversion. It provides enterprise-grade protection with a simple, configurable interface.

**Key Features:**
- 🔐 **Name Obfuscation**: Types, methods, fields, and properties
- 🔒 **String Encryption**: AES-256 encryption for string literals
- 🛡️ **IL2CPP Compatible**: Works perfectly with Unity's IL2CPP
- 🔄 **Backup & Rollback**: Automatic safety net
- 🎮 **Unity Integration**: Editor window + build hooks
- 🤖 **CI/CD Ready**: Command-line interface for automation
- 🔧 **Flexible**: Use built-in or external obfuscators
- 📊 **Detailed Reports**: Complete obfuscation logs

## 📦 Installation

1. **Clone or download this repository**
2. **Add Mono.Cecil** (required for built-in obfuscation):
   ```bash
   # Download Mono.Cecil
   curl -L -o cecil.zip https://www.nuget.org/api/v2/package/Mono.Cecil/0.11.5
   unzip cecil.zip
   cp lib/netstandard2.0/Mono.Cecil.dll Assets/UnityObfuscator/Libs/
   ```
3. **Restart Unity Editor**
4. **Open Tools > Unity Obfuscator**

## 🚀 Quick Start

### Method 1: Editor Window (Recommended for first-time use)

1. Open **Tools > Unity Obfuscator**
2. Configure your settings
3. Click **Run Obfuscation** to test
4. Review the results in Console and `Library/ObfuscatorReports/`
5. Enable **Auto Run on Build** for automatic obfuscation

### Method 2: Automatic Build Integration

Enable auto-run in configuration:
```json
{
  "enableObfuscation": true,
  "autoRunOnBuild": true
}
```

Now obfuscation happens automatically during builds!

### Method 3: Command Line (CI/CD)

```bash
# Run obfuscation
Unity -quit -batchmode -projectPath . \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation

# Rollback
Unity -quit -batchmode -projectPath . \
  -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.Rollback
```

## 📖 Documentation

- **[Complete Documentation](Assets/UnityObfuscator/README.md)** - Full feature guide
- **[Troubleshooting](Assets/UnityObfuscator/TROUBLESHOOTING.md)** - Common issues and solutions
- **[Configuration Reference](Assets/UnityObfuscator/Config/obfuscator_config.json)** - All settings explained

## 🔧 Configuration

Edit `Assets/UnityObfuscator/Config/obfuscator_config.json`:

```json
{
  "enableObfuscation": true,
  "autoRunOnBuild": false,
  "targetAssemblies": ["Assembly-CSharp.dll"],
  "obfuscateTypeNames": true,
  "obfuscateMethodNames": true,
  "obfuscateFieldNames": true,
  "encryptStrings": false,
  "excludedNamespaces": ["UnityEngine", "UnityEditor"]
}
```

## 🎯 How It Works

```
Your C# Code
    ↓
Unity Compilation
    ↓
DLL Assemblies (Library/ScriptAssemblies/)
    ↓
[OBFUSCATION HAPPENS HERE]
    ↓
Obfuscated DLL Assemblies
    ↓
IL2CPP Conversion
    ↓
Native Binary
```

The plugin hooks into Unity's build pipeline and obfuscates assemblies **after C# compilation** but **before IL2CPP conversion**.

## 🔒 What Gets Obfuscated?

### Name Obfuscation
```csharp
// Before
public class PlayerController {
    private int healthPoints;
    public void TakeDamage(int amount) { }
}

// After (example)
public class a {
    private int b;
    public void c(int d) { }
}
```

### String Encryption
```csharp
// Before
string apiKey = "sk_live_abc123xyz";

// After
string apiKey = ObfuscatedString.Decrypt("eJw7Kj4+Pi...");
```

### Protected Elements
- Unity framework classes (UnityEngine.*, UnityEditor.*)
- Serialized fields ([SerializeField])
- Unity callbacks (Start, Update, etc.)
- System libraries
- Your custom exclusions

## 🛡️ Safety Features

### Automatic Backups
Every obfuscation creates a timestamped backup:
```
Library/ObfuscatorBackups/
├── 20250112_143000/
│   ├── Assembly-CSharp.dll
│   └── Assembly-CSharp.pdb
├── 20250112_154500/
└── 20250112_165000/
```

### One-Click Rollback
Something wrong? Restore instantly:
- **Editor**: Tools > Unity Obfuscator > Rollback
- **CLI**: `Unity -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.Rollback`

### Comprehensive Logging
Every operation is logged to `Library/ObfuscatorReports/`:
```
[2025-01-12 14:30:00] Starting Obfuscation Process
[2025-01-12 14:30:01] Found 1 target assemblies
[2025-01-12 14:30:01] Obfuscated 42 types and 215 members
[2025-01-12 14:30:02] Obfuscation Complete: 1/1 assemblies processed
```

## 🎮 Unity Integration

### Editor Window
![Unity Obfuscator Window](https://via.placeholder.com/600x400?text=Unity+Obfuscator+Window)

Access via: **Tools > Unity Obfuscator**

Features:
- Visual configuration
- One-click obfuscation
- Instant rollback
- Configuration management

### Build Hook
Automatically runs during builds:
- IPreprocessBuildWithReport implementation
- Runs after C# compilation
- Before IL2CPP conversion
- Can cancel build on failure

### Menu Integration
```
Tools/
└── Unity Obfuscator/
    ├── [Open Window]
    └── Run Tests
```

## 🤖 CI/CD Integration

### GitHub Actions Example
```yaml
name: Build with Obfuscation

on: [push]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Obfuscate Assemblies
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: Android
          customParameters: -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation
      
      - name: Build IL2CPP
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: Android
```

### GitLab CI Example
```yaml
obfuscate:
  stage: build
  script:
    - /opt/Unity/Editor/Unity -quit -batchmode
      -projectPath $CI_PROJECT_DIR
      -executeMethod UnityObfuscator.Editor.ObfuscatorCLI.RunObfuscation
      -logFile obfuscation.log
  artifacts:
    paths:
      - Library/ObfuscatorReports/
```

## 📊 Tests

Run integration tests:
```
Tools > Unity Obfuscator > Run Tests
```

Tests include:
- Configuration validation
- String encryption/decryption
- Backup manager
- Logging system

## 🆚 Comparison

| Feature | Unity Obfuscator | Commercial Tools |
|---------|------------------|------------------|
| Name Obfuscation | ✅ | ✅ |
| String Encryption | ✅ | ✅ |
| Control Flow | 🔧 External | ✅ Built-in |
| IL2CPP Compatible | ✅ | ✅ |
| Editor Integration | ✅ | ✅ |
| CLI/CI Support | ✅ | ⚠️ Limited |
| Backup/Rollback | ✅ | ⚠️ Limited |
| Open Source | ✅ | ❌ |
| **Price** | **FREE** | **$$$** |

## ⚠️ Important Notes

### What This Plugin CANNOT Do
- **Prevent all reverse engineering** - Determined attackers can still reverse engineer
- **Protect native code** - Only protects managed C# assemblies
- **Work with Assembly Definition References** - Currently targets ScriptAssemblies only
- **Obfuscate already-built games** - Must be integrated during build

### Compatibility
- ✅ Unity 2019.4 - 2022.3+ (LTS versions recommended)
- ✅ IL2CPP builds (Android, iOS, WebGL, Windows, etc.)
- ✅ Mono builds (with caveats)
- ⚠️ .NET Standard 2.0/2.1
- ❌ .NET Framework 3.5 (legacy)

### Performance Impact
- **Build time**: +10-30 seconds (depending on project size)
- **Runtime**: Negligible (except string encryption adds ~0.1ms per decryption)

## 🐛 Troubleshooting

### Common Issues

**"Mono.Cecil library not found"**
- Download from https://www.nuget.org/packages/Mono.Cecil/
- Place in `Assets/UnityObfuscator/Libs/`

**"Assembly not found"**
- Compile scripts first: Assets > Reimport All
- Verify path in configuration

**Serialization issues**
- Disable field obfuscation or add exclusions
- Preserve `[SerializeField]` attributes

**IL2CPP build fails**
- Use rollback immediately
- Disable type obfuscation
- Add problematic namespaces to exclusions

See **[TROUBLESHOOTING.md](Assets/UnityObfuscator/TROUBLESHOOTING.md)** for detailed solutions.

## 📁 Project Structure

```
Assets/UnityObfuscator/
├── Editor/
│   ├── ObfuscatorConfig.cs          # Configuration model
│   ├── ObfuscatorRunner.cs          # Main orchestration
│   ├── CecilObfuscator.cs           # Built-in obfuscator
│   ├── BackupManager.cs             # Backup/rollback
│   ├── ObfuscatorLogger.cs          # Logging
│   ├── ObfuscatorWindow.cs          # Editor UI
│   ├── ObfuscatorBuildHook.cs       # Build integration
│   ├── ObfuscatorCLI.cs             # Command-line interface
│   ├── ObfuscatorTests.cs           # Integration tests
│   └── StringEncryption.cs          # AES utilities
├── Runtime/
│   └── ObfuscatedString.cs          # Runtime decryption
├── Libs/
│   └── Mono.Cecil.dll               # (Download separately)
├── Config/
│   └── obfuscator_config.json       # Default config
├── README.md                         # Full documentation
└── TROUBLESHOOTING.md               # Issue resolution
```

## 🤝 Contributing

Contributions welcome! Areas for improvement:
- Enhanced control flow obfuscation
- Symbol stripping
- Anti-tampering checks
- Performance optimizations
- Additional Unity version support

## 📜 License

MIT License - See LICENSE file for details.

Free for personal and commercial use.

## 🙏 Acknowledgments

Inspired by:
- Unity Obfuscation Pro
- Obfuscator-LLVM
- Mono.Cecil project

Built with:
- [Mono.Cecil](https://github.com/jbevain/cecil) - IL manipulation
- Unity Editor API

## 📮 Support

- 📖 [Documentation](Assets/UnityObfuscator/README.md)
- 🐛 [Issues](https://github.com/Cloud-W/UnityObfuscator/issues)
- 💬 [Discussions](https://github.com/Cloud-W/UnityObfuscator/discussions)

## 🗺️ Roadmap

- [ ] Unity Package Manager distribution
- [ ] Enhanced control flow obfuscation
- [ ] Resource obfuscation
- [ ] Symbol stripping
- [ ] Anti-debugging features
- [ ] Assembly merging
- [ ] Unity 6 support

---

**Made with ❤️ for the Unity community**

If this plugin helps you, consider ⭐ starring the repository!
