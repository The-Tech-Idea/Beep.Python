# Backend Architecture

## Overview

`Beep.Python.Runtime.Host` is a console application that **starts Python backend servers** (HTTP, Pipe, RPC) that expose Infrastructure operations. The host itself doesn't implement Python operations - it delegates to Infrastructure classes and starts servers that can execute them.

## Core Functionality

### 1. Download Python

Uses Infrastructure `IPythonRuntimeManager` to download and setup embedded Python:

```csharp
// Command: init
var runtimeManager = services.GetRequiredService<IPythonRuntimeManager>();
await runtimeManager.Initialize();
var runtimeId = await runtimeManager.CreateManagedRuntime("Default-Embedded", PythonRuntimeType.Embedded);
await runtimeManager.InitializeRuntime(runtimeId);
```

### 2. Manage Virtual Environments

Uses Infrastructure `VirtualEnvManager` and `IVenvManager` to manage virtual environments:

```csharp
// Command: venv create <name>
var env = await _virtualEnvManager.CreateProviderEnvironmentAsync(envName, modelId, cancellationToken);

// Command: venv list
var envPath = _venvManager.GetRegisteredEnvironmentPath(envName);

// Command: venv delete <name>
await _venvManager.DeleteVirtualEnvironment(venvPath, cancellationToken);
```

### 3. Admin Virtual Environment

Special virtual environment for administrative operations:

```csharp
// Command: venv admin
var env = await _virtualEnvManager.CreateProviderEnvironmentAsync("admin", null, cancellationToken);
await _venvManager.InstallProviderPackagesInVenv("admin", pythonExe, adminPackages, cancellationToken);
```

### 4. Start Backend Servers

Uses Infrastructure `PythonServerLauncher` to start backend servers:

```csharp
// Command: start <backendType>
var launcher = new PythonServerLauncher(venvPath, backendType, logger);
await launcher.StartAsync();
```

The backend servers (HTTP/Pipe/RPC) run Python scripts that can:
- Execute Python code
- Import modules
- Create objects
- Call methods
- Access Infrastructure classes through Python.NET

## Exposing VenvManager Operations Through Backends

### Architecture

The backend servers expose operations through `IPythonHostBackend` interface:

1. **HTTP Backend**: REST API endpoints that accept JSON requests
2. **Pipe Backend**: Named pipe IPC communication
3. **RPC Backend**: gRPC service calls

### How VenvManager Operations Are Exposed

When a backend server is started, it runs in a virtual environment. To execute VenvManager operations:

#### Option 1: Direct Infrastructure Access (C# Side)

The host application can directly use Infrastructure classes to manage venvs, then expose results through backend servers:

```csharp
// In C# host application
var venvManager = services.GetRequiredService<IVenvManager>();
var envPath = await venvManager.EnsureProviderEnvironment("my-provider", null, cancellationToken);

// Then use this venv for backend operations
var launcher = new PythonServerLauncher(envPath, PythonBackendType.Http, logger);
```

#### Option 2: Python Script Access (Server Side)

Backend servers can execute Python scripts that use Infrastructure through Python.NET:

```python
# In Python server script
import clr
clr.AddReference("Beep.Python.Runtime")

from Beep.Python.RuntimeEngine.Infrastructure import VenvManager, VirtualEnvManager

# Use Infrastructure classes in Python
venv_manager = VenvManager(...)
env_path = venv_manager.EnsureProviderEnvironment("my-provider", None)
```

#### Option 3: Backend API Endpoints

Backend servers can expose specific endpoints for venv management:

**HTTP Backend:**
```
POST /venv/create
Body: { "providerName": "my-provider", "modelId": null }
Response: { "path": "/path/to/venv", "success": true }
```

**RPC Backend:**
```protobuf
service VenvService {
  rpc CreateEnvironment(CreateEnvRequest) returns (CreateEnvResponse);
  rpc DeleteEnvironment(DeleteEnvRequest) returns (DeleteEnvResponse);
  rpc ListEnvironments(ListEnvRequest) returns (ListEnvResponse);
}
```

### Recommended Approach

1. **Host Application**: Uses Infrastructure directly for venv management (already implemented)
2. **Backend Servers**: Execute Python operations, can access Infrastructure via Python.NET if needed
3. **Separation of Concerns**: 
   - Host = Management & Control
   - Backend Servers = Python Execution

## Commands

| Command | Description | Uses Infrastructure |
|---------|-------------|-------------------|
| `init` | Download/setup Python runtime | `IPythonRuntimeManager` |
| `venv create <name>` | Create virtual environment | `VirtualEnvManager` |
| `venv list` | List virtual environments | `IVenvManager` |
| `venv delete <name>` | Delete virtual environment | `IVenvManager` |
| `venv admin` | Setup admin environment | `VirtualEnvManager`, `IVenvManager` |
| `venv status <name>` | Show environment status | `IVenvManager` |
| `start <backend>` | Start backend server | `PythonServerLauncher` |
| `config` | Check configuration | `IPythonRuntimeManager`, `VirtualEnvManager` |

## Infrastructure Classes Used

- `IPythonRuntimeManager` - Python runtime download and management
- `IVenvManager` - Virtual environment lifecycle management
- `VirtualEnvManager` - High-level virtual environment operations
- `PythonServerLauncher` - Backend server process management
- `IPythonHostBackend` - Backend interface (HTTP/Pipe/RPC implementations)

## Notes

- The host project **starts** backend servers, doesn't implement Python operations
- All Python operations are delegated to Infrastructure classes
- Backend servers can access Infrastructure through Python.NET if needed
- Admin venv is used for administrative operations
- Server venvs are created per-provider as needed
