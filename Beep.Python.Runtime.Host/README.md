# Beep.Python.Runtime.Host

A console application that **starts Python backend servers** (HTTP, Pipe, RPC) and manages Python runtime environments using Infrastructure classes.

## Purpose

The host project:
- **Starts** Python backend servers - doesn't implement Python operations
- **Manages** Python runtime download and virtual environments using Infrastructure
- **Provides** console interface for managing servers and environments

## Features

### 1. Download Python ✅
Uses Infrastructure `IPythonRuntimeManager` to download embedded Python:
```bash
init
```

### 2. Manage Virtual Environments ✅
Uses Infrastructure `VirtualEnvManager` and `IVenvManager`:
```bash
venv create <name>          # Create virtual environment
venv list                   # List all environments
venv delete <name>          # Delete environment
venv admin                  # Setup admin environment
venv status <name>          # Show environment status
```

### 3. Admin Virtual Environment ✅
Special environment for administrative operations:
```bash
venv admin
```

### 4. Start Backend Servers ✅
Uses Infrastructure `PythonServerLauncher` to start servers:
```bash
start http    # Start HTTP backend server
start pipe    # Start Pipe backend server
start rpc     # Start RPC backend server
```

## Using VenvManager Through Backends

### Direct Access (Recommended)

The host application directly uses Infrastructure classes for venv management:
```csharp
// C# host uses Infrastructure directly
var venvManager = services.GetRequiredService<IVenvManager>();
var envPath = await venvManager.EnsureProviderEnvironment("my-provider", null);
```

### Through Backend Servers

Backend servers can execute Python code that accesses Infrastructure via Python.NET:

**Option 1: Python Script Access**
```python
# In Python server script
import clr
clr.AddReference("Beep.Python.Runtime")
from Beep.Python.RuntimeEngine.Infrastructure import VenvManager

venv_manager = VenvManager(...)
env_path = venv_manager.EnsureProviderEnvironment("my-provider", None)
```

**Option 2: Backend API Endpoints**
The backend servers can expose endpoints for venv operations:
- HTTP: `POST /venv/create`
- RPC: `VenvService.CreateEnvironment()`
- Pipe: Command-based protocol

## Architecture

```
┌─────────────────────────────────────────┐
│   Beep.Python.Runtime.Host (Console)    │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Commands (init, venv, start)    │  │
│  └──────────────────────────────────┘  │
│              │                          │
│              ▼                          │
│  ┌──────────────────────────────────┐  │
│  │  Infrastructure Classes          │  │
│  │  - IPythonRuntimeManager         │  │
│  │  - IVenvManager                  │  │
│  │  - VirtualEnvManager             │  │
│  │  - PythonServerLauncher          │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│   Python Backend Servers                │
│                                         │
│  ┌──────────┐  ┌──────────┐  ┌───────┐ │
│  │  HTTP    │  │  Pipe    │  │  RPC  │ │
│  │  Server  │  │  Server  │  │ Server│ │
│  └──────────┘  └──────────┘  └───────┘ │
│                                         │
│  Implement IPythonHostBackend           │
│  - Execute Python code                  │
│  - Access Infrastructure via Python.NET │
└─────────────────────────────────────────┘
```

## Commands

| Command | Description | Infrastructure Used |
|---------|-------------|-------------------|
| `init` | Download/setup Python runtime | `IPythonRuntimeManager` |
| `venv create <name>` | Create virtual environment | `VirtualEnvManager` |
| `venv list` | List virtual environments | `IVenvManager` |
| `venv delete <name>` | Delete virtual environment | `IVenvManager` |
| `venv admin` | Setup admin environment | `VirtualEnvManager`, `IVenvManager` |
| `venv status <name>` | Show environment status | `IVenvManager` |
| `start <backend>` | Start backend server | `PythonServerLauncher` |
| `config` | Check configuration | All Infrastructure classes |
| `status` | Show server status | - |
| `stop` | Stop server | - |

## Infrastructure Classes

The host uses these Infrastructure classes from `Beep.Python.Runtime`:

- **IPythonRuntimeManager** - Download and manage Python runtime
- **IVenvManager** - Virtual environment lifecycle operations
- **VirtualEnvManager** - High-level venv management
- **PythonServerLauncher** - Start backend server processes
- **IPythonHostBackend** - Backend interface (implemented by servers)

## Quick Start

1. **Initialize Python Runtime:**
   ```bash
   init
   ```

2. **Create Admin Environment:**
   ```bash
   venv admin
   ```

3. **Check Configuration:**
   ```bash
   config
   ```

4. **Start Backend Server:**
   ```bash
   start http
   ```

## Notes

- Host **starts** servers, doesn't implement Python operations
- All venv management uses Infrastructure classes directly
- Backend servers can access Infrastructure via Python.NET
- Admin venv is for administrative operations
- Server venvs are created per-provider automatically

## See Also

- `BACKEND_ARCHITECTURE.md` - Detailed architecture documentation
- Infrastructure classes in `Beep.Python.Runtime\Infrastructure`
