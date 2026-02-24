# Spy

[![CI](https://github.com/n7on/spy/actions/workflows/ci.yml/badge.svg)](https://github.com/n7on/spy/actions/workflows/ci.yml) [![PowerShell Gallery Version](https://img.shields.io/powershellgallery/v/Spy)](https://www.powershellgallery.com/packages/Spy) [![PowerShell Gallery Downloads](https://img.shields.io/powershellgallery/dt/Spy)](https://www.powershellgallery.com/packages/Spy) [![License](https://img.shields.io/github/license/n7on/spy)](https://github.com/n7on/spy/blob/main/LICENSE) [![Platform](https://img.shields.io/badge/platform-Windows-blue)]()

A PowerShell module that scans compiled .NET assemblies to discover input surfaces (HTTP endpoints, SignalR hubs, WCF services, gRPC services), check authorization configuration, and flag security issues — all without running the application.

## Installation

```powershell
Install-Module -Name Spy
```

## Usage

### Discover input surfaces

```powershell
# All surfaces
Get-SpySurface -Path .\MyApi.dll

# Filter by surface type
Get-SpySurface -Path .\MyApi.dll -Type HttpEndpoint
Get-SpySurface -Path .\MyApi.dll -Type SignalRMethod
Get-SpySurface -Path .\MyApi.dll -Type WcfOperation
Get-SpySurface -Path .\MyApi.dll -Type GrpcOperation

# Filter by HTTP method
Get-SpySurface -Path .\MyApi.dll -HttpMethod DELETE

# Filter by class name (supports wildcards)
Get-SpySurface -Path .\MyApi.dll -Class User*

# Only authenticated / anonymous surfaces
Get-SpySurface -Path .\MyApi.dll -RequiresAuth
Get-SpySurface -Path .\MyApi.dll -AllowAnonymous
```

### Find security issues

```powershell
# All issues
Find-SpyVulnerability -Path .\MyApi.dll

# Only high-severity issues
Find-SpyVulnerability -Path .\MyApi.dll -MinimumSeverity High

# Filter by surface type
Find-SpyVulnerability -Path .\MyApi.dll -Type WcfOperation

# Detailed view
Find-SpyVulnerability -Path .\MyApi.dll | Format-List
```

## Supported Frameworks

| Framework | Detection Method | Surface Type |
|-----------|-----------------|--------------|
| **ASP.NET Core / Web API** | Controller base class, `[ApiController]`, naming convention | `HttpEndpoint` |
| **SignalR** | `Hub` / `Hub<T>` inheritance | `SignalRMethod` |
| **WCF** | `[ServiceContract]` interfaces + `[OperationContract]` methods | `WcfOperation` |
| **gRPC** | Generated base class with `BindService` | `GrpcOperation` |

## Security Rules

### HTTP Endpoints

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated state-changing endpoint | `DELETE`, `POST`, `PUT`, or `PATCH` without `[Authorize]` |
| **Medium** | Missing authorization declaration | Endpoint has neither `[Authorize]` nor `[AllowAnonymous]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### SignalR Hub Methods

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated hub method | Hub method without `[Authorize]` (directly invocable by clients) |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### WCF Operations

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated WCF operation | Operation without `[PrincipalPermission]` or `[Authorize]` |
| **Low** | Authorize without role | `[PrincipalPermission]` present but no `Role` specified |

### gRPC Operations

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated gRPC operation | Service method without `[Authorize]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

## License

See [LICENSE](LICENSE).
