# Repository Guidelines

## Project Structure & Module Organization
- `Minecraft_updater/` holds the Avalonia client. MVVM pieces live under `Models/`, `Services/`, `ViewModels/`, and `Views/`, with shared assets in `Assets/` and the sample server configuration in `config.ini.example`.
- `Minecraft_updater.Tests/` mirrors the runtime folders (including `Integration/`) and contains all xUnit suites.
- `Minecraft_updater.sln` binds the projects together; build artifacts drop into each project's `bin/` and `obj/` directories.

## Build, Test, and Development Commands
```bash
dotnet restore Minecraft_updater.sln
dotnet build Minecraft_updater.sln -c Debug   # Local debug build
dotnet run --project Minecraft_updater        # Launch the UI with sample config
dotnet test Minecraft_updater.Tests           # Execute the full test suite
dotnet publish Minecraft_updater/Minecraft_updater.csproj \
  -c Release -r win-x64 --self-contained -p:PublishAot=true
```
The GitHub release workflow runs the same restore → build → test chain and publishes self-contained AOT bundles for Linux and Windows when a tag matching `v*` is pushed.

## Coding Style & Naming Conventions
- Target framework is `net9.0`; keep nullable reference types enabled and prefer modern C# features (pattern matching, `async` APIs).
- Use 4-space indentation and let `CSharpier` (wired via MSBuild) format C# files before committing.
- Follow standard .NET naming: PascalCase for classes, interfaces, and public members; camelCase for locals and parameters; suffix view-model types with `ViewModel` and services with `Service`.
- Keep ViewModels thin and delegate I/O or hashing logic to `Services/` to match the current layering.

## Testing Guidelines
- Tests use xUnit, FluentAssertions, and Moq. Name files and classes after the unit under test plus `Tests` (e.g., `PackValidationServiceTests`).
- Integration scenarios belong in `Integration/`. Prefer Arrange-Act-Assert structure and FluentAssertions for readability.
- Generate coverage when touching critical logic: `dotnet test --collect:"XPlat Code Coverage"` then run `reportgenerator` to produce the HTML summary under `TestResults/CoverageReport/`.

## Commit & Pull Request Guidelines
- History mixes conventional commits such as `refactor:` and scoped messages like `test(UpdateService): ...`. Continue using lowercase types (`feat`, `fix`, `refactor`, `test`) with short, imperative descriptions.
- Squash work-in-progress before review. Each PR should explain the change, link any tracked issue, list manual verification (screenshots for UI tweaks), and confirm tests (`dotnet test`) pass.
- For release candidates, reference the intended tag (e.g., `v1.2.0`) so automation can publish the correct artifacts once the tag is pushed.

## Release & Configuration Notes
- Never commit real `config.ini` values; share sanitized examples via `config.ini.example`.
- Keep platform-specific secrets (API tokens, server credentials) in local user secrets or CI environment variables. Update `.github/workflows/release.yml` only when necessary to avoid breaking automated builds.
