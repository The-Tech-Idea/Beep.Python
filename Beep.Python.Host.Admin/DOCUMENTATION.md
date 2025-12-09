# Beep.Python Host Admin - Complete Documentation

## Table of Contents

1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Core Features](#core-features)
4. [Component Details](#component-details)
5. [API Reference](#api-reference)
6. [Configuration](#configuration)
7. [Database Schema](#database-schema)
8. [Security & Authentication](#security--authentication)
9. [Deployment](#deployment)
10. [Troubleshooting](#troubleshooting)

---

## Overview

**Beep.Python Host Admin** is a comprehensive, cross-platform Python and AI Management System built with Flask. It provides a unified web interface for managing Python environments, Large Language Models (LLMs), Retrieval-Augmented Generation (RAG) systems, and associated infrastructure.

### Key Capabilities

- **Python Environment Management**: Discover, create, and manage Python runtimes and virtual environments
- **LLM Management**: Discover, download, configure, and run Large Language Models from HuggingFace and other sources
- **RAG System**: Manage vector databases (FAISS, ChromaDB) and external RAG APIs for document retrieval
- **Package Management**: Search, install, and manage Python packages across environments
- **Server Management**: Start and monitor Python HTTP/RPC servers
- **Task Management**: Track background tasks with real-time progress updates
- **User Management**: Role-based access control (RBAC) with audit logging
- **Data Source Integration**: Sync documents from databases, file systems, and APIs

### Technology Stack

- **Backend**: Flask 3.0+, Flask-SocketIO, Flask-SQLAlchemy
- **Frontend**: Bootstrap 5, Jinja2 templates, JavaScript
- **Database**: SQLite (default), PostgreSQL, SQL Server, Oracle (configurable)
- **AI/ML**: HuggingFace Hub, sentence-transformers, FAISS, ChromaDB
- **Real-time**: WebSocket (SocketIO) for live updates
- **Cross-platform**: Windows, Linux, macOS

---

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Web Browser (Client)                      │
│              Bootstrap 5 UI + JavaScript                      │
└───────────────────────┬───────────────────────────────────────┘
                        │ HTTP/WebSocket
                        │
┌───────────────────────▼───────────────────────────────────────┐
│                    Flask Application                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│  │   Routes     │  │   Services   │  │   Models     │        │
│  │  (Blueprints)│  │  (Business   │  │  (Database)  │        │
│  │              │  │   Logic)     │  │              │        │
│  └──────────────┘  └──────────────┘  └──────────────┘        │
└───────┬──────────────────┬──────────────────┬────────────────┘
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│   Database   │  │  LLM Models  │  │  RAG Systems │
│  (SQLAlchemy)│  │  (HuggingFace)│  │ (FAISS/Chroma)│
└──────────────┘  └──────────────┘  └──────────────┘
```

### Directory Structure

```
Beep.Python.Host.Admin/
├── app/
│   ├── __init__.py              # Application factory
│   ├── config_manager.py        # Configuration management
│   ├── database.py              # Database initialization
│   ├── models/                  # SQLAlchemy models
│   │   ├── core.py              # User, Role, Group, Settings, AuditLog
│   │   └── rag_metadata.py     # RAG collections, documents, sync jobs
│   ├── routes/                  # Flask blueprints
│   │   ├── dashboard.py         # Main dashboard
│   │   ├── runtimes.py          # Python runtime management
│   │   ├── environments.py      # Virtual environment management
│   │   ├── packages.py          # Package management
│   │   ├── servers.py           # Server management
│   │   ├── llm.py               # LLM management UI
│   │   ├── rag.py               # RAG management UI
│   │   ├── openai_api.py        # OpenAI-compatible API
│   │   ├── tasks.py             # Task management
│   │   ├── api.py               # REST API endpoints
│   │   └── setup.py             # Setup wizard
│   ├── services/                # Business logic services
│   │   ├── llm_manager.py       # LLM model management
│   │   ├── rag_service.py       # RAG service (multi-provider)
│   │   ├── inference_service.py # Model inference
│   │   ├── environment_manager.py # Virtual environment management
│   │   ├── runtime_manager.py   # Python runtime discovery
│   │   ├── server_manager.py    # Server process management
│   │   ├── task_manager.py      # Background task tracking
│   │   ├── auth_service.py      # Authentication & RBAC
│   │   ├── hardware_service.py  # Hardware detection
│   │   ├── huggingface_service.py # HuggingFace integration
│   │   ├── sync_scheduler.py    # RAG sync job scheduler
│   │   └── rag_providers/       # RAG provider implementations
│   │       ├── base.py          # Base provider interface
│   │       ├── faiss_provider.py
│   │       ├── chromadb_provider.py
│   │       └── external_api.py
│   ├── scripts/                 # Utility scripts
│   │   └── inference_subprocess.py
│   └── utils/                   # Utility functions
│       └── permissions.py
├── templates/                   # Jinja2 HTML templates
├── config/                      # Configuration files
│   └── rag_config.json
├── migrations/                  # Database migrations
├── instance/                    # Runtime data (database files)
├── requirements.txt             # Python dependencies
├── run.py                       # Application entry point
├── init_database.py             # Database initialization script
└── README.md
```

### Data Flow

1. **User Request** → Flask Route Handler
2. **Route Handler** → Service Layer (business logic)
3. **Service Layer** → Database Models / External APIs
4. **Response** → JSON (API) or HTML Template (Web UI)
5. **Real-time Updates** → WebSocket (SocketIO)

---

## Core Features

### 1. Python Runtime Management

**Purpose**: Discover and manage Python installations on the system.

**Features**:
- Automatic discovery of system Python installations
- Embedded Python installation support
- Python version detection and validation
- Runtime metadata (version, path, executable)

**Service**: `RuntimeManager`

**Routes**: `/runtimes/*`

### 2. Virtual Environment Management

**Purpose**: Create, configure, and manage isolated Python environments.

**Features**:
- Create virtual environments from any Python runtime
- Pre-configured templates (Data Science, Flask, FastAPI, ML/AI)
- Package installation and management
- Environment isolation and dependency tracking
- Cross-platform support (Windows, Linux, macOS)

**Service**: `EnvironmentManager`

**Routes**: `/environments/*`

### 3. LLM Management

**Purpose**: Discover, download, configure, and run Large Language Models.

**Features**:
- **Model Discovery**: Browse HuggingFace models with filtering
- **Model Download**: Download models with progress tracking
- **Model Storage**: Local model repository management
- **Inference**: Run models with configurable parameters
- **Hardware Detection**: GPU/CPU detection and recommendations
- **Model Recommendations**: AI-powered model suggestions based on use case
- **Environment Management**: Isolated Python environments for LLM dependencies
- **Chat Interface**: Interactive chat with loaded models
- **OpenAI-Compatible API**: `/v1/chat/completions` endpoint

**Service**: `LLMManager`, `InferenceService`, `HuggingFaceService`

**Routes**: `/llm/*`, `/v1/*` (OpenAI API)

**Supported Formats**: GGUF, GGML, SafeTensors, PyTorch, ONNX

### 4. RAG (Retrieval-Augmented Generation) System

**Purpose**: Manage vector databases and document retrieval for RAG applications.

**Features**:
- **Multiple Providers**:
  - **FAISS**: Fast, memory-efficient similarity search
  - **ChromaDB**: Feature-rich vector database
  - **External API**: Integration with external RAG services
- **Collection Management**: Create, configure, and manage document collections
- **Document Upload**: Upload and index documents
- **Context Retrieval**: Query-based document retrieval
- **Data Source Sync**: Automated syncing from:
  - SQL databases (PostgreSQL, MySQL, SQL Server, Oracle, SQLite)
  - File systems
  - REST APIs
  - CSV files
- **Scheduled Sync Jobs**: Cron-based or interval-based document synchronization
- **Access Control**: User/group-based permissions
- **Metadata Management**: Document metadata and indexing

**Service**: `RAGService`, `RAGEnvironmentManager`, `SyncScheduler`

**Routes**: `/rag/*`

### 5. Package Management

**Purpose**: Search, install, and manage Python packages.

**Features**:
- PyPI package search
- Package information and metadata
- Installation across virtual environments
- Dependency resolution
- Version management

**Service**: Integrated with `EnvironmentManager`

**Routes**: `/packages/*`

### 6. Server Management

**Purpose**: Start and manage Python HTTP/RPC servers.

**Features**:
- Server process management
- Real-time status monitoring
- Log viewing
- Start/stop control

**Service**: `ServerManager`

**Routes**: `/servers/*`

### 7. Task Management

**Purpose**: Track background tasks with progress updates.

**Features**:
- Task creation and tracking
- Progress monitoring
- Step-by-step task execution
- Real-time updates via WebSocket
- Task history

**Service**: `TaskManager`

**Routes**: `/tasks/*`

### 8. User Management & RBAC

**Purpose**: Role-based access control and user management.

**Features**:
- User accounts with authentication
- Role-based permissions (Admin, Power User, User, Guest)
- User groups
- Access privileges for resources
- Audit logging
- Session management

**Service**: `AuthService`

**Models**: `User`, `Role`, `Group`, `AccessPrivilege`, `AuditLog`

---

## Component Details

### Services

#### LLMManager

Manages LLM models: discovery, download, storage, metadata.

**Key Methods**:
- `get_local_models()`: List downloaded models
- `download_model()`: Download model from HuggingFace
- `get_storage_stats()`: Get storage usage statistics
- `delete_model()`: Remove model from storage

#### RAGService

Unified RAG service supporting multiple providers.

**Key Methods**:
- `set_provider()`: Switch between FAISS, ChromaDB, External API
- `add_documents()`: Index documents
- `retrieve_context()`: Query for relevant documents
- `configure()`: Configure provider settings

**Providers**:
1. **FAISS Provider**: Local FAISS index with sentence-transformers
2. **ChromaDB Provider**: Local ChromaDB database
3. **External API Provider**: Delegates to external RAG API

#### InferenceService

Manages model inference and chat interactions.

**Key Methods**:
- `load_model()`: Load model for inference
- `generate()`: Generate text from prompt
- `chat()`: Interactive chat interface
- `get_loaded_models()`: List currently loaded models

#### EnvironmentManager

Manages Python virtual environments.

**Key Methods**:
- `create_environment()`: Create new virtual environment
- `list_environments()`: List all environments
- `install_packages()`: Install packages in environment
- `delete_environment()`: Remove environment

#### SyncScheduler

Manages scheduled RAG document synchronization jobs.

**Features**:
- Cron-based scheduling
- Interval-based scheduling
- Job history tracking
- Cross-platform support (Windows, Linux, macOS)

### Database Models

#### Core Models (`app/models/core.py`)

- **User**: User accounts with authentication
- **Role**: RBAC roles with permissions
- **Group**: User groups
- **Setting**: Application settings (key-value store)
- **AuditLog**: System audit trail

#### RAG Models (`app/models/rag_metadata.py`)

- **Collection**: RAG collection metadata
- **Document**: Document metadata
- **AccessPrivilege**: Resource access control
- **DataSource**: Data source configurations
- **SyncJob**: Scheduled sync job definitions
- **SyncJobRun**: Sync job execution history

---

## API Reference

### REST API Base URL

All REST API endpoints are under `/api/v1/`

### Authentication

Currently, the API uses session-based authentication. API key authentication can be added.

### Endpoints

#### Runtimes

- `GET /api/v1/runtimes` - List all Python runtimes
- `GET /api/v1/runtimes/{id}` - Get runtime details
- `POST /api/v1/runtimes/install-embedded` - Install embedded Python

#### Environments

- `GET /api/v1/environments` - List all environments
- `POST /api/v1/environments` - Create new environment
- `GET /api/v1/environments/{id}` - Get environment details
- `DELETE /api/v1/environments/{id}` - Delete environment
- `GET /api/v1/environments/{id}/packages` - List packages
- `POST /api/v1/environments/{id}/packages` - Install packages
- `DELETE /api/v1/environments/{id}/packages/{name}` - Uninstall package

#### LLM

- `GET /api/v1/llm/models` - List local models
- `POST /api/v1/llm/models/download` - Download model
- `GET /api/v1/llm/models/{id}` - Get model details
- `POST /api/v1/llm/inference/load` - Load model for inference
- `POST /api/v1/llm/inference/generate` - Generate text
- `POST /api/v1/llm/inference/chat` - Chat with model

#### RAG

- `GET /api/v1/rag/collections` - List collections
- `POST /api/v1/rag/collections` - Create collection
- `GET /api/v1/rag/collections/{id}` - Get collection details
- `POST /api/v1/rag/collections/{id}/documents` - Upload documents
- `POST /api/v1/rag/query` - Query for context
- `GET /api/v1/rag/data-sources` - List data sources
- `POST /api/v1/rag/sync-jobs` - Create sync job

#### Servers

- `GET /api/v1/servers` - List all servers
- `POST /api/v1/servers` - Start new server
- `GET /api/v1/servers/{id}` - Get server details
- `DELETE /api/v1/servers/{id}` - Stop server

#### Tasks

- `GET /api/v1/tasks` - List all tasks
- `GET /api/v1/tasks/{id}` - Get task details
- `POST /api/v1/tasks/{id}/cancel` - Cancel task

#### System

- `GET /api/v1/health` - Health check
- `GET /api/v1/info` - System information

### OpenAI-Compatible API

The application provides an OpenAI-compatible API endpoint:

- `POST /v1/chat/completions` - Chat completions (OpenAI format)

**Example Request**:
```json
{
  "model": "local-model-name",
  "messages": [
    {"role": "user", "content": "Hello!"}
  ]
}
```

---

## Configuration

### Application Configuration

Configuration is managed via `ConfigManager` and stored in:
- `~/.beep-llm/config/app_config.json` (default)

**Environment Variables**:
- `BEEP_PYTHON_HOME`: Base directory for application data (default: `~/.beep-llm`)
- `SECRET_KEY`: Flask secret key (required for production)
- `DEBUG`: Enable debug mode (default: `true`)
- `HOST`: Bind address (default: `127.0.0.1`)
- `PORT`: Server port (default: `5000`)
- `DATABASE_URL`: Database connection string (optional)

### Database Configuration

Supported databases:
- **SQLite** (default): `sqlite:///beep_python.db`
- **PostgreSQL**: `postgresql://user:pass@host:port/dbname`
- **SQL Server**: `mssql+pyodbc://user:pass@host:port/dbname?driver=...`
- **Oracle**: `oracle+cx_oracle://user:pass@host:port/?service_name=...`

Database is configured during setup wizard or via environment variables.

### RAG Configuration

RAG settings are stored in `config/rag_config.json`:

```json
{
  "enabled": true,
  "provider_type": "faiss",
  "max_context_length": 4000,
  "embedding_model": "all-MiniLM-L6-v2",
  "data_path": null
}
```

### LLM Configuration

LLM models are stored in:
- `~/.beep-llm/models/` (default)

Model metadata and configurations are stored in the database.

---

## Database Schema

### Core Tables

#### users
- `id` (PK)
- `username` (unique)
- `password_hash`
- `email` (unique)
- `display_name`
- `is_admin`
- `is_active`
- `role_id` (FK → roles)
- `created_at`
- `last_login`

#### roles
- `id` (PK)
- `name` (unique)
- `description`
- `permissions` (JSON)
- `is_system`
- `created_at`

#### groups
- `id` (PK)
- `name` (unique)
- `description`
- `created_at`
- `created_by` (FK → users)

#### settings
- `key` (PK)
- `value`
- `description`
- `is_encrypted`
- `updated_at`

#### audit_log
- `id` (PK)
- `action`
- `resource_type`
- `resource_id`
- `user_id` (FK → users)
- `details`
- `ip_address`
- `created_at`

### RAG Tables

#### collections
- `id` (PK)
- `collection_id` (unique) - ID in vector DB
- `name`
- `description`
- `provider` (faiss, chromadb, external_api)
- `owner_id` (FK → users)
- `document_count`
- `is_public`
- `created_at`
- `updated_at`
- `metadata_json` (JSON)

#### documents
- `id` (PK)
- `document_id`
- `collection_id` (FK → collections)
- `source` (file path or URL)
- `title`
- `content_hash`
- `chunk_count`
- `uploaded_by` (FK → users)
- `created_at`
- `metadata_json` (JSON)

#### rag_data_sources
- `id` (PK)
- `name`
- `source_type` (sqlite, postgresql, mysql, mssql, file_system, api, csv)
- `description`
- Connection fields (host, port, database, username, password, etc.)
- `target_collection_id`
- `is_active`
- `created_at`
- `updated_at`
- `created_by` (FK → users)

#### rag_sync_jobs
- `id` (PK)
- `name`
- `description`
- `data_source_id` (FK → rag_data_sources)
- `collection_id`
- `schedule_type` (manual, interval, cron)
- `interval_minutes`
- `cron_expression`
- `sync_mode` (full, incremental)
- `is_active`
- `last_run_at`
- `last_status`
- `created_at`
- `updated_at`

#### rag_sync_job_runs
- `id` (PK)
- `job_id` (FK → rag_sync_jobs)
- `started_at`
- `completed_at`
- `status`
- `documents_added`
- `documents_updated`
- `documents_deleted`
- `documents_failed`
- `error_message`
- `log`

#### access_privileges
- `id` (PK)
- `resource_type` (collection, document)
- `resource_id`
- `user_id` (FK → users)
- `group_id` (FK → groups)
- `access_level` (read, write, admin)
- `granted_by` (FK → users)
- `created_at`
- `expires_at`

---

## Security & Authentication

### Authentication

- Session-based authentication
- Password hashing with Werkzeug
- Session timeout configuration

### Authorization (RBAC)

**Roles**:
- **Admin**: Full system access
- **Power User**: Extended permissions (model management, RAG admin)
- **User**: Standard user permissions
- **Guest**: Read-only access

**Permissions**:
- Model management
- RAG collection management
- Environment management
- User management
- System settings

### Access Control

- Resource-level permissions (collections, documents)
- User and group-based access
- Public/private collections
- Audit logging for all actions

### Security Best Practices

1. **Change default admin password** immediately after first login
2. **Use strong SECRET_KEY** in production
3. **Enable HTTPS** in production
4. **Encrypt sensitive settings** (database passwords, API keys)
5. **Regular security updates** for dependencies
6. **Audit log monitoring**

---

## Deployment

### Development

```bash
# 1. Clone repository
git clone <repository-url>
cd Beep.Python.Host.Admin

# 2. Create virtual environment
python -m venv .venv

# 3. Activate environment
# Windows:
.venv\Scripts\Activate.ps1
# Linux/macOS:
source .venv/bin/activate

# 4. Install dependencies
pip install -r requirements.txt

# 5. Initialize database
python init_database.py

# 6. Run application
python run.py
```

### Production

#### Using Gunicorn (Linux/macOS)

```bash
pip install gunicorn
gunicorn -w 4 -b 0.0.0.0:5000 --worker-class eventlet -k eventlet "run:app"
```

#### Using Waitress (Windows)

```bash
pip install waitress
waitress-serve --host=0.0.0.0 --port=5000 "run:app"
```

#### Environment Variables

```bash
export SECRET_KEY="your-secret-key-here"
export DEBUG="false"
export HOST="0.0.0.0"
export PORT="5000"
export BEEP_PYTHON_HOME="/path/to/data"
```

#### Database Setup

For production, use PostgreSQL or another production database:

```bash
export DATABASE_URL="postgresql://user:pass@host:port/dbname"
```

#### Reverse Proxy (Nginx)

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Docker (Future)

Docker support can be added for containerized deployment.

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Errors

**Problem**: Cannot connect to database

**Solutions**:
- Check database file permissions (SQLite)
- Verify database connection string
- Ensure database server is running (PostgreSQL, etc.)
- Check firewall rules

#### 2. Model Download Failures

**Problem**: Model downloads fail or timeout

**Solutions**:
- Check internet connection
- Verify HuggingFace token (if required)
- Increase timeout settings
- Check available disk space

#### 3. RAG Provider Errors

**Problem**: FAISS or ChromaDB provider fails

**Solutions**:
- Ensure RAG environment is properly set up
- Check Python dependencies in RAG environment
- Verify data path permissions
- Check provider configuration

#### 4. Virtual Environment Creation Fails

**Problem**: Cannot create virtual environments

**Solutions**:
- Verify Python runtime is accessible
- Check disk space
- Verify write permissions
- Ensure virtualenv is installed

#### 5. Port Already in Use

**Problem**: Port 5000 is already in use

**Solutions**:
- Change port via environment variable: `export PORT=5001`
- Kill process using port 5000
- Use different port in configuration

### Logs

Application logs are output to console. For production, configure proper logging:

```python
import logging
logging.basicConfig(
    filename='app.log',
    level=logging.INFO,
    format='%(asctime)s %(levelname)s %(message)s'
)
```

### Debug Mode

Enable debug mode for detailed error messages:

```bash
export DEBUG=true
python run.py
```

**Warning**: Never enable debug mode in production!

---

## Additional Resources

- **Database Documentation**: See `DATABASE.md`
- **Migrations**: See `migrations/README.md`
- **API Examples**: Check route handlers in `app/routes/`
- **Service Documentation**: See docstrings in `app/services/`

---

## License

MIT License - See LICENSE file for details.

---

**Last Updated**: 2024
**Version**: 1.0.0

