# Spy

A PowerShell module that scans compiled .NET assemblies to discover HTTP endpoints and SignalR hub methods, check authorization configuration, and flag security issues — all without running the application.

## Installation

```powershell
Install-Module -Name Spy
```

## Usage

### Discover input surfaces

```powershell
# All surfaces (HTTP endpoints + SignalR methods)
Get-SpySurface -Path .\MyApi.dll

# Only HTTP endpoints
Get-SpySurface -Path .\MyApi.dll -Type HttpEndpoint

# Only SignalR hub methods
Get-SpySurface -Path .\MyApi.dll -Type SignalRMethod

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

# Only SignalR issues
Find-SpyVulnerability -Path .\MyApi.dll -Type SignalRMethod

# Detailed view
Find-SpyVulnerability -Path .\MyApi.dll | Format-List
```

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

## License

See [LICENSE](LICENSE).
