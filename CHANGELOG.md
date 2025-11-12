# Changelog

All notable changes to the Unity Obfuscator project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-12

### Added
- Initial release of Unity Obfuscator plugin
- **Core Obfuscation Features**:
  - Name obfuscation for types, methods, fields, and properties
  - String constant encryption using AES-256
  - Whitelist system for excluding Unity framework classes
  - Attribute-based exclusion (e.g., `[Preserve]`, `[SerializeField]`)
  - Support for external obfuscator integration

- **Unity Integration**:
  - Editor window for visual configuration (`Tools > Unity Obfuscator`)
  - Automatic build pipeline integration via IPreprocessBuildWithReport
  - Command-line interface for CI/CD workflows
  - Assembly definition files for proper Unity compilation

- **Safety Features**:
  - Automatic backup system with timestamped folders
  - One-click rollback functionality
  - Configurable backup retention (keeps last 5 backups)
  - Comprehensive error handling

- **Configuration System**:
  - JSON-based configuration files
  - Live configuration validation
  - Support for custom configurations in CI/CD
  - Extensive exclusion rules (namespaces, types, assemblies)

- **Logging & Reporting**:
  - Detailed operation logs
  - Obfuscation reports with statistics
  - Separate logs for build, manual, and CLI executions
  - Configurable verbosity levels

- **Testing**:
  - Integration test suite
  - Sample test script demonstrating obfuscation
  - Tests for configuration, encryption, backup, and logging

- **Documentation**:
  - Comprehensive README with quick start guide
  - Detailed troubleshooting guide
  - Configuration reference
  - Contributing guidelines
  - MIT License

### Technical Details
- Built on Mono.Cecil for IL manipulation
- Uses .NET Standard 2.0 for maximum compatibility
- Supports Unity 2019.4 - 2022.3+ (LTS versions)
- Compatible with IL2CPP builds on all platforms

### Known Limitations
- Requires manual installation of Mono.Cecil library
- Generic constraints may not be preserved correctly in some cases
- Async/await patterns have limited support
- Reflection-heavy code requires extensive exclusions
- Hot reload not supported with obfuscated assemblies

## [Unreleased]

### Planned Features
- Enhanced built-in control flow obfuscation
- Resource name obfuscation
- Anti-tampering checks
- Symbol stripping
- Assembly merging
- Unity Package Manager distribution
- Unity 6 support

---

## Version History

### Version Numbering
- **Major (X.0.0)**: Breaking changes or major new features
- **Minor (0.X.0)**: New features, backward compatible
- **Patch (0.0.X)**: Bug fixes and minor improvements

### Support Policy
- Latest version receives active support
- Previous major version receives security fixes for 6 months
- Older versions are unsupported

---

[1.0.0]: https://github.com/Cloud-W/UnityObfuscator/releases/tag/v1.0.0
