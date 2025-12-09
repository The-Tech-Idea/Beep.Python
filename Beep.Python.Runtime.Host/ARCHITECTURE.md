# Beep.Python.Runtime.Host Architecture

## Overview

`Beep.Python.Runtime.Host` is a **server host** that starts backend servers (HTTP, Pipe, RPC). These backend servers expose Infrastructure operations (including VenvManager) that other projects connect to as clients.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│   Beep.Python.Runtime.Host (Server Host)                    │
│                                                              │
│  1. Download Python (using Infrastructure)                  │
│  2. Manage Virtual Environments (using Infrastructure)      │
│  3. Start Backend Servers (HTTP/Pipe/RPC)                  │
│  4. Expose Infrastructure Operations through Servers        │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ Starts & Manages
                         ▼
┌─────────────────────────────────────────────────────────────┐
│   Backend Servers (HTTP, Pipe, RPC)                         │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  HTTP Server │  │  Pipe Server │  │  RPC Server  │     │
│  │  (FastAPI)   │  │  (IPC)       │  │  (gRPC)      │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│                                                              │
│  Expose Infrastructure Operations:                          │
│  - VenvManager operations                                   │
│  - Python code execution                                    │
│  - Module imports                                           │
│  - Object creation/method calls                             │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ Clients Connect Via
                         ▼
┌─────────────────────────────────────────────────────────────┐
│   Client Projects (Beep.LLM.Console, etc.)                  │
│                                                              │
│  Use IPythonHostBackend implementations:                    │
│  - PythonHostHttp (connects to HTTP server)                 │
│  - PythonHostPipe (connects to Pipe server)                 │
│  - PythonHostRpc (connects to RPC server)                   │
│                                                              │
│  Execute operations through backend:                        │
│  - VenvManager.EnsureProviderEnvironment()                  │
│  - Python code execution                                    │
│  - All Infrastructure operations                            │
└─────────────────────────────────────────────────────────────┘
```

## Key Concept

**The host project REPLACES direct IPythonHostBackend usage** - instead, projects connect to backend servers that the host starts.

### Before (Direct Usage):
```csharp
// In Beep.LLM.Console - direct usage
var backend = new PythonHostPythonNet(logger);
await backend.InitializeAsync();
```

### After (Through Backend Servers):
```csharp
// In Beep.LLM.Console - connect to backend server
var backend = new PythonHostHttp("http://localhost:5678", logger);
await backend.InitializeAsync();

// Now all operations go through the backend server
var venvPath = await ExecuteVenvOperationThroughBackend(...);
```

## Backend Server Responsibilities

Backend servers must expose:

1. **VenvManager Operations** - Create/manage virtual environments
2. **Python Execution** - Execute Python code
3. **Infrastructure Access** - Access Infrastructure classes via Python.NET

## Host Project Responsibilities

1. **Download Python** - Using Infrastructure `IPythonRuntimeManager`
2. **Manage Virtual Environments** - Using Infrastructure `IVenvManager` and `VirtualEnvManager`
3. **Create Admin Virtual Environment** - For administrative operations
4. **Start Backend Servers** - HTTP, Pipe, or RPC servers
5. **Expose Infrastructure Through Servers** - Backend servers can access Infrastructure classes

## Implementation

### 1. Host Starts Backend Server

```csharp
// In host project
var launcher = new PythonServerLauncher(venvPath, PythonBackendType.Http, logger);
await launcher.StartAsync();
// Server now running at http://localhost:5678
```

### 2. Backend Server Exposes VenvManager Operations

The Python server scripts need to:
- Import Infrastructure classes via Python.NET
- Expose endpoints/commands for VenvManager operations
- Execute operations and return results

### 3. Client Projects Connect

```csharp
// In Beep.LLM.Console or other projects
var backend = PythonBackendFactory.CreateHttpBackend("http://localhost:5678", logger);
await backend.InitializeAsync();

// Execute VenvManager operations through backend
var pythonCode = @"
import clr
clr.AddReference('Beep.Python.Runtime')
from Beep.Python.RuntimeEngine.Infrastructure import VenvManager
# ... use VenvManager
";
var result = await backend.EvaluateAsync<string>(pythonCode);
```

## Backend Server Endpoints (HTTP Example)

```
POST /venv/create
Body: { "providerName": "my-provider", "modelId": null }
Response: { "path": "/path/to/venv", "success": true }

POST /venv/delete
Body: { "venvPath": "/path/to/venv" }
Response: { "success": true }

POST /execute
Body: { "code": "python code here" }
Response: { "result": "execution result" }
```

## Commands

The host provides commands to:

- `init` - Download Python runtime
- `venv create <name>` - Create virtual environment (using Infrastructure)
- `venv admin` - Setup admin virtual environment
- `start <backend>` - Start backend server (HTTP/Pipe/RPC)
- `status` - Check backend server status
- `stop` - Stop backend server

## Benefits

1. **Centralized Python Management** - One host manages all Python environments
2. **Remote Access** - Multiple projects can connect to same backend server
3. **Separation** - Host manages, backends execute, clients consume
4. **Flexibility** - Choose HTTP, Pipe, or RPC based on needs

## Next Steps

1. Update Python server scripts to expose VenvManager operations
2. Create service layer in host to expose Infrastructure through backends
3. Update documentation for client projects on how to connect
