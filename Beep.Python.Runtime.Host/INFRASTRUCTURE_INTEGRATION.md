# Infrastructure Integration

## Overview

The `Beep.Python.Runtime.Host` now uses Infrastructure classes from `Beep.Python.Runtime` to create and manage environments automatically.

## Changes Made

### 1. ServerLauncher Uses Infrastructure

**Before:**
- Accepted a venv path directly
- No environment management

**After:**
- Uses `IPythonRuntimeManager` to manage Python runtimes
- Uses `IVenvManager` to create/manage virtual environments
- Uses `VirtualEnvManager` from Infrastructure for high-level management
- Automatically creates environments if they don't exist
- Installs required packages automatically

### 2. SimpleVenvManager Implementation

Created `SimpleVenvManager.cs` that implements `IVenvManager` interface:
- Creates virtual environments using Python's `venv` module
- Manages provider environments
- Installs packages via pip
- Tracks created environments

### 3. Program.cs Integration

Updated to:
- Create `PythonRuntimeManager` from Infrastructure
- Create `SimpleVenvManager` that implements `IVenvManager`
- Pass Infrastructure managers to `ServerLauncher`
- Made `--venv` option optional (environments are created automatically)

## Usage

### Automatic Environment Creation

```bash
# Environment will be created automatically
dotnet run -- start --backend Http

# Or specify existing venv
dotnet run -- start --backend Http --venv C:\path\to\venv
```

### Infrastructure Flow

```
Program.cs
  ↓ Creates
IPythonRuntimeManager (PythonRuntimeManager)
  ↓ Creates
IVenvManager (SimpleVenvManager)
  ↓ Passed to
ServerLauncher
  ↓ Uses
VirtualEnvManager (Infrastructure)
  ↓ Creates/Manages
Virtual Environment
  ↓ Installs
Required Packages
  ↓ Launches
Python Server
```

## Benefits

1. **Automatic Environment Management**: No need to manually create venvs
2. **Package Installation**: Required packages installed automatically
3. **Infrastructure Integration**: Uses same managers as rest of system
4. **Runtime Discovery**: Automatically finds and uses Python runtimes
5. **Flexibility**: Can use existing venv or create new one

## Architecture

```
┌─────────────────────────────────────┐
│  Program.cs                         │
│  - Creates Infrastructure managers │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  PythonRuntimeManager               │
│  (Infrastructure)                   │
│  - Manages Python runtimes          │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  SimpleVenvManager                  │
│  (Implements IVenvManager)          │
│  - Creates virtual environments     │
│  - Installs packages                │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  ServerLauncher                     │
│  - Uses Infrastructure managers     │
│  - Creates environment if needed    │
│  - Installs packages                │
│  - Launches Python server           │
└─────────────────────────────────────┘
```

## Environment Creation

When no venv is provided, Infrastructure:
1. Initializes runtime manager
2. Finds available Python runtime
3. Creates virtual environment at: `~/.beep-python/venvs/{provider-name}`
4. Installs packages from `requirements.txt`
5. Launches server with created environment

## Configuration

Environment location can be customized by modifying `SimpleVenvManager`:
- Default: `~/.beep-python/venvs/{provider-name}`
- Can be overridden by providing `--venv` option
