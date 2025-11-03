# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Minecraft_updater is an automatic update tool for Minecraft servers, designed to help server administrators automatically distribute mod and configuration updates to clients. The application is built with:
- **.NET 9.0** with Avalonia UI (cross-platform desktop framework)
- **MVVM architecture** using CommunityToolkit.Mvvm
- **AOT (Ahead-of-Time) compilation** enabled for optimized performance
- **xUnit** for testing with FluentAssertions and Moq

The application supports MD5 file verification, fuzzy file name matching for deletion, and self-updating capabilities.

## Build & Test Commands

### Building
```bash
# Build the entire solution
dotnet build Minecraft_updater.sln

# Build release configuration
dotnet build -c Release

# Publish with AOT compilation (platform-specific)
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r win-x64
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test file
dotnet test --filter "FullyQualifiedName~PackTests"

# Run tests in watch mode
dotnet watch test --project Minecraft_updater.Tests
```

### Code Formatting
The project uses CSharpier for code formatting:
```bash
# Format code (happens automatically on build via CSharpier.MsBuild)
dotnet build
```

## Architecture & Code Structure

### Application Entry Points

The application uses **command-line arguments** to determine which window to display:
- `/Check_Update` - Launches the updater window (UpdaterWindow) to sync Minecraft files
- `/updatepackMaker` - Launches the pack maker window (UpdatepackMakerWindow) to generate file lists
- `/Check_updaterVersion` - Checks for and installs updater self-updates (UpdateSelfWindow)
- No arguments - Shows usage information

See [App.axaml.cs:41-91](Minecraft_updater/App.axaml.cs#L41-L91) for the argument routing logic.

### Core Update Mechanism

The update process centers around a **Pack List file** (plain text format) that defines files to sync:

**Pack format**: `Path||MD5||URL`
- `#` prefix = Delete file (fuzzy match using delimiters: `+`, `-`, `_`)
- `:` prefix = Download only when file doesn't exist
- Regular entry = Sync file (compare MD5, download if different)

Example:
```
mods/Botania-1.20.jar||ABC123||http://example.com/Botania-1.20.jar
#mods/OldMod||DEF456||
:config/optional.cfg||789GHI||http://example.com/optional.cfg
```

The Pack model and parsing logic is in [Models/Pack.cs](Minecraft_updater/Models/Pack.cs).

### Key Service Layer Components

**UpdateService** ([Services/UpdateService.cs](Minecraft_updater/Services/UpdateService.cs)):
- `CheckUpdateAsync()` - Checks for updater self-updates from GitHub Releases
- Version comparison using `Version.CompareTo()`
- Reads version info from GitHub API: `https://api.github.com/repos/flier268/Minecraft_updater/releases/latest`
- Parses release tag (e.g., "v1.2.3") and uses release body as update message
- `GetAssetNameForCurrentPlatform()` - Determines the platform-specific asset name (win-x64, linux-x64, osx-x64)
- Automatically finds the matching download URL from GitHub Release assets based on the current OS
- Download URL is stored in `UpdateMessage.SHA1` field for backward compatibility

**PrivateFunction** ([Services/PrivateFunction.cs](Minecraft_updater/Services/PrivateFunction.cs)):
- `GetMD5(filepath)` - Computes MD5 hash for file verification
- `DownloadFileAsync()` - Downloads files with progress tracking
- Temp file creation/deletion utilities

### MVVM Pattern

ViewModels inherit from `ViewModelBase` and use CommunityToolkit.Mvvm attributes:
- `[ObservableProperty]` - Generates bindable properties
- `[RelayCommand]` - Generates command implementations
- ViewModels are in [ViewModels/](Minecraft_updater/ViewModels/)
- Views (AXAML + code-behind) are in [Views/](Minecraft_updater/Views/)

**UpdaterWindowViewModel** ([ViewModels/UpdaterWindowViewModel.cs](Minecraft_updater/ViewModels/UpdaterWindowViewModel.cs)):
- Main sync logic in `CheckPackAsync()` (line 116+)
- Reads config from `config.ini` (URL, auto-close settings)
- File deletion with fuzzy matching using delimiter chars: `+`, `-`, `_` (line 177-219)
- MD5-based file verification and download (line 221-268)

**UpdatepackMakerWindowViewModel** ([ViewModels/UpdatepackMakerWindowViewModel.cs](Minecraft_updater/ViewModels/UpdatepackMakerWindowViewModel.cs)):
- Generates Pack List files by scanning directories
- Calculates MD5 hashes for all files
- Supports drag-and-drop for folder selection

**UpdateSelfWindowViewModel** ([ViewModels/UpdateSelfWindowViewModel.cs](Minecraft_updater/ViewModels/UpdateSelfWindowViewModel.cs)):
- Handles self-update process for the application
- Downloads platform-specific zip files from GitHub Releases
- Extracts the update and replaces the current executable
- Includes rollback mechanism if update fails
- No SHA1 verification (relies on GitHub's secure distribution)

### Configuration

The application reads from `config.ini`:
- `scUrl` - URL to the Pack List file
- `AutoClose_AfterFinishd` - Auto-close after sync completion
- `LogFile` - Enable/disable logging
- `updatepackMaker_BaseURL` - Base URL for generated pack lists

See [Models/IniFile.cs](Minecraft_updater/Models/IniFile.cs) for INI handling.

### File Deletion Logic

Fuzzy matching for file deletion (used for versioned mods like `Botania-1.20.1.jar`):
1. Takes delete entry path (e.g., `mods/Botania`)
2. Finds files starting with that path
3. Next character must be a delimiter: `+`, `-`, or `_`
4. Deletes if MD5 doesn't match (prevents deleting identical files)

See [UpdaterWindowViewModel.cs:177-219](Minecraft_updater/ViewModels/UpdaterWindowViewModel.cs#L177-L219).

## AOT Compilation Considerations

The project has `<PublishAot>true</PublishAot>` enabled:
- Avoid reflection-heavy patterns
- The `DisableAvaloniaDataAnnotationValidation()` method removes incompatible validation plugins
- Suppression attributes are used for AOT-safe code patterns (see [App.axaml.cs:138-155](Minecraft_updater/App.axaml.cs#L138-L155))

## Testing Approach

Tests are organized by layer:
- `Models/` - Test data models and parsing (e.g., PackTests for Pack format parsing)
- `Services/` - Test core services (UpdateService, PrivateFunction, Log)
- `ViewModels/` - Test ViewModel logic with Moq for mocking

Use FluentAssertions for readable assertions:
```csharp
result.Should().BeEquivalentTo(expected);
result.Should().NotBeNull();
```

## Common Development Patterns

### Dispatcher Usage
UI updates from background threads must use `Dispatcher.UIThread`:
```csharp
await Dispatcher.UIThread.InvokeAsync(() => {
    // UI updates here
});
```

### Async/Await
Most I/O operations are async:
- File downloads: `DownloadFileAsync()`
- Update checks: `CheckUpdateAsync()`
- Always use `await` properly to avoid blocking

### Error Handling
- Log errors with color coding in ViewModels: `AddLog(message, "#FF0000")`
- Use try-catch blocks around I/O operations
- Continue execution after non-critical failures

## Platform Notes

The application supports Linux, Windows, and macOS through Avalonia:
- X11 configuration in [Program.cs:30](Minecraft_updater/Program.cs#L30)
- Platform detection via `UsePlatformDetect()`
- Skia rendering backend for graphics
