# Contributing to Unity Obfuscator

Thank you for your interest in contributing to Unity Obfuscator! This document provides guidelines and instructions for contributing.

## How to Contribute

### Reporting Bugs

If you find a bug, please open an issue with:
- A clear title and description
- Steps to reproduce the issue
- Expected vs actual behavior
- Unity version and platform
- Relevant logs from `Library/ObfuscatorReports/`
- Configuration file (if applicable)

### Suggesting Features

Feature requests are welcome! Please include:
- Clear use case
- Why this feature would be useful
- Any implementation ideas

### Code Contributions

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/my-feature`
3. **Make your changes**
4. **Test thoroughly**:
   - Test in Unity Editor
   - Test with different Unity versions if possible
   - Test CLI functionality
   - Run integration tests
5. **Commit with clear messages**
6. **Push to your fork**
7. **Open a Pull Request**

## Development Setup

### Requirements
- Unity 2019.4 or later
- Mono.Cecil 0.11.5
- .NET Standard 2.0 compatible IDE (Visual Studio, Rider, VS Code)

### Getting Started
```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/UnityObfuscator.git

# Create a branch
cd UnityObfuscator
git checkout -b feature/my-feature

# Open in Unity
Unity -projectPath .
```

### Project Structure
- `Assets/UnityObfuscator/Editor/` - Editor scripts
- `Assets/UnityObfuscator/Runtime/` - Runtime scripts
- `Assets/UnityObfuscator/Config/` - Configuration files
- `Assets/UnityObfuscator/Libs/` - Third-party libraries

## Coding Guidelines

### Style
- Follow Unity C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and small
- Use proper error handling

### Example
```csharp
/// <summary>
/// Obfuscate a specific assembly
/// </summary>
/// <param name="assemblyPath">Full path to the assembly file</param>
/// <returns>True if obfuscation succeeded, false otherwise</returns>
public bool ObfuscateAssembly(string assemblyPath)
{
    if (string.IsNullOrEmpty(assemblyPath))
    {
        logger.LogError("Assembly path is null or empty");
        return false;
    }
    
    try
    {
        // Implementation
        return true;
    }
    catch (Exception ex)
    {
        logger.LogException(ex);
        return false;
    }
}
```

### Documentation
- Update README.md for user-facing changes
- Update TROUBLESHOOTING.md for common issues
- Add inline comments for complex logic
- Update configuration documentation

## Testing

### Manual Testing Checklist
- [ ] Plugin compiles without errors
- [ ] Editor window opens and displays correctly
- [ ] Configuration saves and loads properly
- [ ] Manual obfuscation works
- [ ] Auto-run on build works
- [ ] CLI commands work
- [ ] Rollback functionality works
- [ ] Reports are generated correctly

### Integration Tests
Run the built-in tests:
```
Tools > Unity Obfuscator > Run Tests
```

### Test in Different Scenarios
- Empty project
- Project with existing code
- IL2CPP builds
- Different Unity versions
- Different platforms (Android, iOS, Windows, etc.)

## Pull Request Process

1. **Update documentation** for any user-facing changes
2. **Add tests** if adding new functionality
3. **Ensure all tests pass**
4. **Update CHANGELOG.md** with your changes
5. **Describe your changes** clearly in the PR description
6. **Link related issues** if applicable

### PR Title Format
- `feat: Add feature X`
- `fix: Fix bug Y`
- `docs: Update documentation`
- `refactor: Improve code structure`
- `test: Add tests for Z`

### PR Description Template
```markdown
## Description
Brief description of changes

## Motivation
Why is this change needed?

## Changes
- Change 1
- Change 2

## Testing
How was this tested?

## Screenshots (if applicable)
Add screenshots for UI changes

## Checklist
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] All tests pass
- [ ] CHANGELOG.md updated
```

## Areas for Contribution

### High Priority
- [ ] Enhanced control flow obfuscation (built-in)
- [ ] Better Unity callback detection
- [ ] Performance optimizations
- [ ] Unity 6 support
- [ ] More comprehensive tests

### Medium Priority
- [ ] Resource name obfuscation
- [ ] Symbol stripping
- [ ] Assembly merging
- [ ] Anti-tampering checks
- [ ] Package Manager distribution

### Low Priority
- [ ] GUI improvements
- [ ] Additional logging options
- [ ] More configuration options
- [ ] Documentation improvements

## Code Review Process

All submissions require review. We use GitHub PRs for this purpose.

Reviews focus on:
- Code quality and maintainability
- Performance implications
- Security considerations
- Unity compatibility
- Documentation completeness

## Community

- Be respectful and constructive
- Help others when you can
- Share your use cases and feedback
- Suggest improvements

## Questions?

- Open an issue with the `question` label
- Start a discussion in GitHub Discussions
- Check existing issues and documentation first

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

Thank you for contributing to Unity Obfuscator! 🎉
