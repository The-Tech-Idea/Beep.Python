# Integration with Beep.Python.Host.Admin

## Overview

**Beep.Python.MLStudio** integrates with **Beep.Python.Host.Admin** via HTTP API calls. This means:

1. **No code copying needed** - MLStudio communicates with Host Admin over HTTP
2. **Host Admin must be running** - It runs as a separate service
3. **Automatic detection** - The launcher can find and start Host Admin automatically

## How It Works

### Architecture

```
┌─────────────────────────┐         HTTP API         ┌──────────────────────────┐
│                         │  ──────────────────────> │                          │
│  Beep.Python.MLStudio   │                          │  Beep.Python.Host.Admin  │
│  (Port 5001)            │  <────────────────────── │  (Port 5000)             │
│                         │                          │                          │
│  - Web UI               │                          │  - Environment Manager    │
│  - Project Management   │                          │  - Virtual Environments  │
│  - Experiment Tracking  │                          │  - Package Installation  │
└─────────────────────────┘                          └──────────────────────────┘
```

### Communication Flow

1. **User creates ML project in MLStudio**
2. **MLStudio calls Host Admin API**:
   ```
   POST http://127.0.0.1:5000/api/v1/environments
   {
     "name": "mlstudio_my_project",
     "packages": ["numpy", "pandas", "scikit-learn"]
   }
   ```
3. **Host Admin creates virtual environment** and installs packages
4. **MLStudio stores project info** and uses the environment for training

## Finding Host Admin

The launcher (`run_mlstudio.py`) automatically searches for Host Admin in these locations:

1. **Sibling directory**: `../Beep.Python.Host.Admin` (most common)
2. **Subdirectory**: `./Beep.Python.Host.Admin`
3. **Home directory**: `~/Beep.Python.Host.Admin`
4. **Parent's parent**: If MLStudio is in `Beep.Python/Beep.Python.MLStudio`

### Recommended Project Structure

```
Beep.Python/
├── Beep.Python.Host.Admin/     ← Host Admin (must be running)
│   ├── run.py
│   ├── app/
│   └── ...
└── Beep.Python.MLStudio/        ← MLStudio (this project)
    ├── run_mlstudio.py
    ├── app/
    └── ...
```

## Starting Host Admin

### Option 1: Automatic (Recommended)

The MLStudio launcher can automatically start Host Admin if:
- Host Admin is found in a known location
- You answer 'y' when prompted

### Option 2: Manual

Start Host Admin in a separate terminal:

```bash
# Windows
cd Beep.Python.Host.Admin
run.bat

# Linux/macOS
cd Beep.Python.Host.Admin
./run.sh
```

### Option 3: Always Running

Run Host Admin as a service or keep it running in the background.

## Configuration

The connection is configured in `.env`:

```bash
# Host Admin URL (default: http://127.0.0.1:5000)
HOST_ADMIN_URL=http://127.0.0.1:5000

# Optional API key if authentication is enabled
HOST_ADMIN_API_KEY=
```

## API Endpoints Used

MLStudio uses these Host Admin API endpoints:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/health` | GET | Health check |
| `/api/v1/environments` | GET | List environments |
| `/api/v1/environments` | POST | Create environment |
| `/api/v1/environments/{name}` | GET | Get environment details |
| `/api/v1/environments/{name}` | DELETE | Delete environment |
| `/api/v1/environments/{name}/packages` | POST | Install packages |
| `/api/v1/environments/{name}/packages` | GET | List packages |

## Troubleshooting

### "Cannot connect to Host Admin"

**Problem**: MLStudio can't reach Host Admin API

**Solutions**:
1. Ensure Host Admin is running: `http://127.0.0.1:5000/api/v1/health`
2. Check `HOST_ADMIN_URL` in `.env` file
3. Verify Host Admin is on the correct port (default: 5000)
4. Check firewall settings

### "Host Admin not found"

**Problem**: Launcher can't find Host Admin directory

**Solutions**:
1. Place Host Admin in a sibling directory: `../Beep.Python.Host.Admin`
2. Or update the launcher to search your specific location
3. Or start Host Admin manually before running MLStudio

### "Environment creation failed"

**Problem**: Can't create virtual environment via Host Admin

**Solutions**:
1. Check Host Admin logs for errors
2. Ensure Python is installed and accessible
3. Verify disk space is available
4. Check Host Admin has proper permissions

## Benefits of HTTP Integration

✅ **Separation of Concerns**: Each application has a single responsibility  
✅ **No Code Duplication**: No need to copy environment management code  
✅ **Independent Updates**: Update Host Admin or MLStudio independently  
✅ **Scalability**: Host Admin can serve multiple MLStudio instances  
✅ **Flexibility**: Host Admin can be on a different machine (change URL)

## Alternative: Embedded Host Admin

If you want to bundle Host Admin with MLStudio, you could:

1. **Copy Host Admin code** into MLStudio (not recommended - maintenance burden)
2. **Use Host Admin as a library** (requires refactoring Host Admin)
3. **Keep HTTP integration** (current approach - recommended)

The HTTP API approach is cleaner and more maintainable.

## Network Configuration

### Local Development (Default)
- Host Admin: `http://127.0.0.1:5000`
- MLStudio: `http://127.0.0.1:5001`

### Remote Host Admin
If Host Admin is on a different machine:

```bash
# In MLStudio .env
HOST_ADMIN_URL=http://192.168.1.100:5000
```

### Docker/Container Setup
Both services can run in separate containers and communicate via network.

---

**Note**: The HTTP API integration means Host Admin and MLStudio are **loosely coupled**. This is a design feature, not a limitation. It allows for better separation, testing, and deployment flexibility.

