# Contributing to Spy

If you want to contribute to the source you're highly welcome!

## Prerequisites

* [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
* PowerShell 5.1+ or PowerShell 7+

## Build

```powershell
dotnet build
```

For a release build:

```powershell
dotnet build -c Release
```

## Test

```powershell
dotnet test
```

## Load the Module Locally

```powershell
Import-Module ./out/Spy
Get-SpySurface -Path ./MyApi.dll
```

## Project Structure

```
spy/
├── src/
│   ├── Spy.Core/              # Core library (netstandard2.0)
│   │   ├── Contracts/         # Data models
│   │   ├── Helpers/           # Reflection utilities
│   │   └── Services/          # Discovery and analysis logic
│   └── Spy.PowerShell/        # PowerShell module (netstandard2.0)
│       ├── Commands/          # Cmdlets
│       └── Formatters/        # ps1xml formatting
├── tests/
│   └── Spy.Core.Tests/        # xUnit tests
│       ├── Fixtures/          # Fake ASP.NET types and sample controllers/hubs
│       ├── Helpers/           # ReflectionHelper tests
│       └── Services/          # Discovery, scanner, and analyzer tests
├── docs/                      # Documentation
└── Spy.sln
```

## How It Works

Spy loads .NET assemblies via `System.Reflection` and scans for types that represent input surfaces:

**HTTP Endpoints** — Classes inheriting from `ControllerBase`, `Controller`, or `ApiController`; classes with `[ApiController]`; or classes named `*Controller` with public action methods. Routes are resolved by combining controller-level and action-level templates, with support for `[controller]` and `[action]` tokens.

**SignalR Hub Methods** — Classes inheriting from `Hub` or `Hub<T>`. Public instance methods are discovered, excluding lifecycle methods like `OnConnectedAsync`. Routes use conventional naming (strip "Hub" suffix) since actual `MapHub<T>("/route")` calls aren't discoverable via reflection.

## Adding a New Discovery Type

To add a new surface type (e.g. gRPC, minimal APIs):

1. Add the type to `SurfaceType` enum in `src/Spy.Core/Contracts/SurfaceType.cs`
2. Create a new class extending `InputSurface` in `src/Spy.Core/Contracts/`
3. Create a new `IDiscovery` implementation in `src/Spy.Core/Services/`
4. Add security analysis rules in `AssemblyScanner.AnalyzeSecurityIssues`
5. Wire it up in `ScannerFactory.Create()`
6. Add formatting views in `Spy.Format.ps1xml`

## Releasing a New Version

1. Update version in `src/Spy.PowerShell/Spy.psd1`:
   ```powershell
   ModuleVersion = '0.2.0'
   ```

2. Update release notes in `src/Spy.PowerShell/Spy.psd1`

3. Update `CHANGELOG.md`

4. Commit and push the changes

5. Create and push a git tag:
   ```bash
   git tag v0.2.0
   git push origin v0.2.0
   ```

GitHub Actions will automatically publish to PowerShell Gallery when a tag is pushed.
