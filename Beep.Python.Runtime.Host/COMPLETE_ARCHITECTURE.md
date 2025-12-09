# Complete Architecture: Host as Backend Server Provider

## Overview

`Beep.Python.Runtime.Host` **starts backend servers** (HTTP, Pipe, RPC) that expose Infrastructure operations. Other projects (like `Beep.LLM.Console`) connect to these backend servers as clients using `IPythonHostBackend` implementations.

## Architecture Flow

```
┌──────────────────────────────────────────────────────────┐
│  Beep.Python.Runtime.Host                                │
│  (Server Host - Starts Backend Servers)                  │
│                                                           │
│  1. Download Python (Infrastructure)                     │
│  2. Manage Virtual Environments (Infrastructure)         │
│  3. Create Admin Virtual Environment                     │
│  4. Start Backend Servers                                │
└──────────────────────────────────────────────────────────┘
                         │
                         │ Starts & Manages
                         ▼
┌──────────────────────────────────────────────────────────┐
│  Backend Servers (HTTP, Pipe, RPC)                       │
│                                                           │
│  ┌──────────────────────────────────────────────────┐   │
│  │  HTTP Server (FastAPI)                           │   │
│  │  - POST /api/import                              │   │
│  │  - POST /api/eval                                │   │
│  │  - POST /api/venv/create (NEW - VenvManager)    │   │
│  │  - POST /api/venv/delete (NEW - VenvManager)    │   │
│  └──────────────────────────────────────────────────┘   │
│                                                           │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Pipe Server (Named Pipes)                       │   │
│  │  - Command: "import"                             │   │
│  │  - Command: "eval"                               │   │
│  │  - Command: "venv_create" (NEW)                 │   │
│  └──────────────────────────────────────────────────┘   │
│                                                           │
│  ┌──────────────────────────────────────────────────┐   │
│  │  RPC Server (gRPC)                               │   │
│  │  - ImportModule()                                │   │
│  │  - Evaluate()                                    │   │
│  │  - VenvCreate() (NEW)                            │   │
│  └──────────────────────────────────────────────────┘   │
│                                                           │
│  All servers can access Infrastructure via Python.NET:   │
│  - Import Infrastructure classes                        │
│  - Use VenvManager, VirtualEnvManager, etc.             │
│  - Execute Python code that uses Infrastructure         │
└──────────────────────────────────────────────────────────┘
                         │
                         │ Clients Connect Via
                         ▼
┌──────────────────────────────────────────────────────────┐
│  Client Projects (Beep.LLM.Console, etc.)                │
│                                                           │
│  Connect using IPythonHostBackend:                      │
│                                                           │
│  var backend = new PythonHostHttp(                       │
│      "http://localhost:5678", logger);                   │
│  await backend.InitializeAsync();                        │
│                                                           │
│  // Execute VenvManager operations through backend:     │
│  var code = @"                                           │
│      import clr                                          │
│      clr.AddReference('Beep.Python.Runtime')             │
│      from Beep.Python.RuntimeEngine.Infrastructure       │
│          import VenvManager                              │
│      # ... use VenvManager                              │
│  ";                                                      │
│  var result = await backend.EvaluateAsync<string>(code);│
│                                                           │
│  OR use backend endpoints directly:                      │
│  POST /api/venv/create                                   │
│  { "providerName": "my-provider", "modelId": null }     │
└──────────────────────────────────────────────────────────┘
```

## How It Works

### 1. Host Starts Backend Server

```csharp
// In Beep.Python.Runtime.Host
var launcher = new PythonServerLauncher(venvPath, PythonBackendType.Http, logger);
await launcher.StartAsync();
// Server now running at http://localhost:5678
```

### 2. Backend Server Exposes Infrastructure Operations

The Python server scripts (http_server.py, pipe_server.py, rpc_server.py) run in a virtual environment and can:

- Import .NET assemblies via Python.NET
- Access Infrastructure classes (VenvManager, VirtualEnvManager, etc.)
- Execute Python code that uses Infrastructure
- Expose endpoints/commands for Infrastructure operations

### 3. Client Projects Connect and Use Backend

```csharp
// In Beep.LLM.Console or other projects
var backend = PythonBackendFactory.CreateHttpBackend("http://localhost:5678", logger);
await backend.InitializeAsync();

// Execute operations through backend
var pythonCode = @"
import clr
clr.AddReference('Beep.Python.Runtime')
from Beep.Python.RuntimeEngine.Infrastructure import VenvManager

# Use VenvManager through backend
venv_manager = VenvManager(...)
result = venv_manager.EnsureProviderEnvironment('my-provider', None)
result
";

var venvPath = await backend.EvaluateAsync<string>(pythonCode);
```

## Backend Server Endpoints (HTTP Example)

### Standard IPythonHostBackend Endpoints:
- `POST /api/import` - Import Python module
- `POST /api/eval` - Evaluate Python code
- `POST /api/create` - Create Python object
- `POST /api/call` - Call Python method

### New VenvManager Endpoints:
- `POST /api/venv/create` - Create virtual environment
- `POST /api/venv/delete` - Delete virtual environment
- `GET /api/venv/list` - List virtual environments
- `POST /api/venv/install` - Install packages in venv

## Key Points

1. **Host Project**: Starts backend servers, manages Python runtime, manages virtual environments
2. **Backend Servers**: Expose Infrastructure operations through HTTP/Pipe/RPC
3. **Client Projects**: Connect to backend servers, use IPythonHostBackend implementations
4. **Infrastructure**: Accessible through backend servers via Python.NET

## Implementation Status

✅ Host starts backend servers using Infrastructure
✅ Backend servers can execute Python code
🔄 Backend servers need endpoints for VenvManager operations
🔄 Client projects need to connect to backend servers instead of direct IPythonHostBackend

## Next Steps

1. Update Python server scripts to add VenvManager endpoints
2. Ensure Python.NET is available in backend server venvs
3. Update client projects to connect to backend servers
4. Document connection endpoints for client projects
