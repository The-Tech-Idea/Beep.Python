# Setup Guide for Beep.Python.Runtime.Host

## Overview

This console application provides Python backend servers that integrate with `Beep.Python.Runtime` Infrastructure. The servers are implemented using:

- **FastAPI** for HTTP backend
- **Named Pipes/Unix Sockets** for IPC backend  
- **gRPC** for RPC backend

## Prerequisites

1. .NET 8.0 SDK or later
2. Python 3.8+ with a virtual environment
3. Python packages (installed in venv)

## Setup Steps

### 1. Build the Project

```bash
cd Beep.Python.Runtime.Host
dotnet build
```

### 2. Install Python Dependencies

Activate your virtual environment and install required packages:

```bash
# Activate virtual environment
# Windows:
venv\Scripts\activate
# Linux/macOS:
source venv/bin/activate

# Install dependencies
cd PythonServers
pip install -r requirements.txt
```

### 3. Generate gRPC Code (for RPC backend)

If you plan to use the gRPC/RPC backend, generate the protobuf code:

```bash
cd PythonServers
python -m grpc_tools.protoc -I. --python_out=. --grpc_python_out=. python_service.proto
```

This generates:
- `python_service_pb2.py`
- `python_service_pb2_grpc.py`

## Usage

### Start HTTP Server

```bash
dotnet run -- start --backend Http --venv C:\path\to\venv
```

With custom port:
```bash
dotnet run -- start --backend Http --venv C:\path\to\venv --port 8080
```

### Start Named Pipe Server

```bash
dotnet run -- start --backend Pipe --venv C:\path\to\venv
```

### Start gRPC Server

```bash
dotnet run -- start --backend Rpc --venv C:\path\to\venv --port 50051
```

### List Available Runtimes

```bash
dotnet run -- list
```

## Integration with Infrastructure

The servers launched by this host can be used with Infrastructure classes:

```csharp
using Beep.Python.RuntimeEngine;
using Beep.Python.RuntimeEngine.Infrastructure;

// HTTP backend
var httpBackend = PythonBackendFactory.CreateHttpBackend(
    "http://localhost:5678", 
    logger);

// Pipe backend  
var pipeBackend = PythonBackendFactory.CreatePipeBackend(
    "beep-python-pipe", 
    logger);

// RPC backend
var rpcBackend = PythonBackendFactory.CreateRpcBackend(
    "localhost:50051", 
    logger);
```

## Troubleshooting

### Python Scripts Not Found

The application automatically extracts Python scripts from embedded resources on first run. If scripts are missing:

1. Check that scripts are embedded in the assembly:
   ```bash
   dotnet build
   ```

2. Scripts are extracted to: `{AppContext.BaseDirectory}/python-servers/`

### gRPC Import Errors

If you get import errors for `python_service_pb2`:

1. Make sure you've generated the protobuf files (see step 3 above)
2. Check that the generated files are in the `PythonServers` directory
3. Verify the Python path includes the script directory

### Virtual Environment Issues

Ensure:
- Virtual environment path is correct
- Python executable exists in `venv/Scripts/python.exe` (Windows) or `venv/bin/python` (Linux/macOS)
- All required packages are installed in the venv

## Development

### Project Structure

```
Beep.Python.Runtime.Host/
├── Program.cs                    # Console app entry point
├── ServerLauncher.cs             # Server process launcher
├── PythonServers/                # Python server scripts
│   ├── http_server.py            # FastAPI HTTP server
│   ├── pipe_server.py            # Named Pipe server
│   ├── rpc_server.py             # gRPC server
│   ├── python_service.proto      # gRPC service definition
│   └── requirements.txt          # Python dependencies
└── README.md                     # Documentation
```

### Adding New Features

1. Python servers are embedded as resources
2. Scripts are automatically extracted on first run
3. Servers run in separate processes with virtual environment isolation

## Notes

- Servers run independently and can be stopped with Ctrl+C
- Each server type uses different communication protocols
- HTTP server uses FastAPI with automatic OpenAPI documentation at `/docs`
- All servers support health checks for monitoring
