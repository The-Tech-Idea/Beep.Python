# Beep.Python Host Admin

> A comprehensive, cross-platform Python and AI Management System

[![Python](https://img.shields.io/badge/Python-3.8+-blue.svg)](https://www.python.org/)
[![Flask](https://img.shields.io/badge/Flask-3.0+-green.svg)](https://flask.palletsprojects.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey.svg)]()

**Beep.Python Host Admin** is a professional web application for managing Python environments, Large Language Models (LLMs), Retrieval-Augmented Generation (RAG) systems, and AI infrastructure. Built with Flask and designed for cross-platform deployment.

---

## 🚀 Features

### 🐍 Python Environment Management
- **Runtime Discovery**: Automatically discover Python installations on your system
- **Virtual Environments**: Create, configure, and manage isolated Python environments
- **Package Management**: Search, install, and manage packages from PyPI
- **Environment Templates**: Pre-configured templates for Data Science, Flask, FastAPI, ML/AI, and more
- **Cross-Platform**: Works on Windows, Linux, and macOS

### 🤖 LLM (Large Language Model) Management
- **Model Discovery**: Browse and search models from HuggingFace Hub
- **Model Download**: Download models with progress tracking and resume support
- **Local Model Storage**: Manage your local model repository
- **Inference Engine**: Run models with configurable parameters
- **Hardware Detection**: Automatic GPU/CPU detection and recommendations
- **Model Recommendations**: AI-powered suggestions based on use case and hardware
- **Chat Interface**: Interactive chat interface for testing models
- **OpenAI-Compatible API**: `/v1/chat/completions` endpoint for integration
- **Environment Isolation**: Dedicated Python environments for LLM dependencies

### 📚 RAG (Retrieval-Augmented Generation) System
- **Multiple Providers**: Choose from FAISS, ChromaDB, or External API
- **Collection Management**: Create and manage document collections
- **Document Indexing**: Upload and index documents for retrieval
- **Context Retrieval**: Query-based document retrieval with relevance scoring
- **Data Source Sync**: Automated synchronization from:
  - SQL databases (PostgreSQL, MySQL, SQL Server, Oracle, SQLite)
  - File systems
  - REST APIs
  - CSV files
- **Scheduled Sync Jobs**: Cron-based or interval-based document synchronization
- **Access Control**: User and group-based permissions for collections
- **Metadata Management**: Rich metadata and indexing capabilities

### 🖥️ Server Management
- Start and manage Python HTTP/RPC servers
- Real-time status monitoring
- Log viewing and management
- Process control (start/stop)

### 📋 Task Management
- Background task tracking
- Real-time progress updates via WebSocket
- Step-by-step task execution
- Task history and logs

### 👥 User Management & Security
- **Role-Based Access Control (RBAC)**: Admin, Power User, User, Guest roles
- **User Groups**: Organize users into groups
- **Access Privileges**: Fine-grained resource permissions
- **Audit Logging**: Complete audit trail of all actions
- **Session Management**: Secure session handling

### 🔌 REST API
- Full JSON API for programmatic access
- OpenAI-compatible API endpoint
- WebSocket support for real-time updates
- Comprehensive API documentation

### 🎨 Modern UI
- Professional dark-themed Bootstrap 5 interface
- Responsive design for desktop and mobile
- Real-time updates via WebSocket
- Intuitive navigation and workflows

---

## 📋 Requirements

- **Python**: 3.8 or higher
- **Operating System**: Windows, Linux, or macOS
- **RAM**: 4GB minimum (8GB+ recommended for LLM features)
- **Disk Space**: 10GB+ for models and environments
- **Internet**: Required for model downloads and package installation

---

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Beep.Python.Host.Admin
```

### 2. Create Virtual Environment

```bash
python -m venv .venv
```

### 3. Activate Virtual Environment

**Windows (PowerShell):**
```powershell
.venv\Scripts\Activate.ps1
```

**Windows (CMD):**
```cmd
.venv\Scripts\activate.bat
```

**Linux/macOS:**
```bash
source .venv/bin/activate
```

### 4. Install Dependencies

```bash
pip install -r requirements.txt
```

### 5. Initialize Database

```bash
python init_database.py
```

This creates:
- ✅ All database tables
- ✅ Default roles (Admin, Power User, User, Guest)
- ✅ Admin user (username: `admin`, password: `admin123`)
- ✅ Default group and settings

**⚠️ Important**: Change the admin password immediately after first login!

### 6. Run the Application

```bash
python run.py
```

The application will be available at: **http://127.0.0.1:5000**

### 7. First Login

- **Username**: `admin`
- **Password**: `admin123`
- **⚠️ Change password immediately!**

---

## 📁 Project Structure

```
Beep.Python.Host.Admin/
├── app/                          # Application code
│   ├── __init__.py              # Application factory
│   ├── config_manager.py        # Configuration management
│   ├── database.py              # Database initialization
│   ├── models/                  # SQLAlchemy models
│   │   ├── core.py              # User, Role, Group, Settings, AuditLog
│   │   └── rag_metadata.py     # RAG collections, documents, sync jobs
│   ├── routes/                  # Flask blueprints (URL handlers)
│   │   ├── dashboard.py         # Main dashboard
│   │   ├── runtimes.py          # Python runtime management
│   │   ├── environments.py     # Virtual environment management
│   │   ├── packages.py         # Package management
│   │   ├── servers.py          # Server management
│   │   ├── llm.py              # LLM management
│   │   ├── rag.py              # RAG management
│   │   ├── openai_api.py       # OpenAI-compatible API
│   │   ├── tasks.py            # Task management
│   │   ├── api.py              # REST API endpoints
│   │   └── setup.py            # Setup wizard
│   ├── services/               # Business logic
│   │   ├── llm_manager.py      # LLM model management
│   │   ├── rag_service.py      # RAG service (multi-provider)
│   │   ├── inference_service.py # Model inference
│   │   ├── environment_manager.py # Virtual environment management
│   │   ├── runtime_manager.py  # Python runtime discovery
│   │   ├── server_manager.py   # Server process management
│   │   ├── task_manager.py     # Background task tracking
│   │   ├── auth_service.py     # Authentication & RBAC
│   │   ├── hardware_service.py # Hardware detection
│   │   ├── huggingface_service.py # HuggingFace integration
│   │   ├── sync_scheduler.py   # RAG sync job scheduler
│   │   └── rag_providers/      # RAG provider implementations
│   │       ├── base.py
│   │       ├── faiss_provider.py
│   │       ├── chromadb_provider.py
│   │       └── external_api.py
│   ├── scripts/                # Utility scripts
│   └── utils/                  # Utility functions
├── templates/                  # Jinja2 HTML templates
├── config/                     # Configuration files
│   └── rag_config.json
├── migrations/                 # Database migrations
├── instance/                   # Runtime data (database files)
├── requirements.txt            # Python dependencies
├── run.py                     # Application entry point
├── init_database.py           # Database initialization
├── README.md                  # This file
└── DOCUMENTATION.md           # Complete documentation
```

---

## 🔧 Configuration

### Environment Variables

Create a `.env` file or set environment variables:

```bash
# Flask Configuration
SECRET_KEY=your-secret-key-here  # Required for production
DEBUG=true                        # Set to false in production
HOST=127.0.0.1                    # Bind address
PORT=5000                         # Server port

# Application Paths
BEEP_PYTHON_HOME=~/.beep-llm      # Base directory for data

# Database (Optional - defaults to SQLite)
DATABASE_URL=sqlite:///beep_python.db
```

### Database Configuration

The application supports multiple databases:

- **SQLite** (default): `sqlite:///beep_python.db`
- **PostgreSQL**: `postgresql://user:pass@host:port/dbname`
- **SQL Server**: `mssql+pyodbc://user:pass@host:port/dbname?driver=...`
- **Oracle**: `oracle+cx_oracle://user:pass@host:port/?service_name=...`

Configure during setup wizard or via `DATABASE_URL` environment variable.

---

## 📡 API Reference

### REST API

All REST API endpoints are under `/api/v1/`:

#### Runtimes
- `GET /api/v1/runtimes` - List all Python runtimes
- `GET /api/v1/runtimes/{id}` - Get runtime details

#### Environments
- `GET /api/v1/environments` - List all environments
- `POST /api/v1/environments` - Create new environment
- `GET /api/v1/environments/{id}` - Get environment details
- `DELETE /api/v1/environments/{id}` - Delete environment
- `POST /api/v1/environments/{id}/packages` - Install packages

#### LLM
- `GET /api/v1/llm/models` - List local models
- `POST /api/v1/llm/models/download` - Download model
- `POST /api/v1/llm/inference/load` - Load model for inference
- `POST /api/v1/llm/inference/generate` - Generate text

#### RAG
- `GET /api/v1/rag/collections` - List collections
- `POST /api/v1/rag/collections` - Create collection
- `POST /api/v1/rag/collections/{id}/documents` - Upload documents
- `POST /api/v1/rag/query` - Query for context

#### System
- `GET /api/v1/health` - Health check
- `GET /api/v1/info` - System information

### OpenAI-Compatible API

The application provides an OpenAI-compatible endpoint:

**Endpoint**: `POST /v1/chat/completions`

**Example Request**:
```json
{
  "model": "local-model-name",
  "messages": [
    {"role": "user", "content": "Hello, how are you?"}
  ],
  "temperature": 0.7,
  "max_tokens": 100
}
```

**Example Response**:
```json
{
  "id": "chatcmpl-123",
  "object": "chat.completion",
  "created": 1677652288,
  "choices": [{
    "index": 0,
    "message": {
      "role": "assistant",
      "content": "Hello! I'm doing well, thank you for asking."
    },
    "finish_reason": "stop"
  }],
  "usage": {
    "prompt_tokens": 9,
    "completion_tokens": 12,
    "total_tokens": 21
  }
}
```

---

## 🎯 Use Cases

### 1. Python Environment Management
- Manage multiple Python projects with isolated environments
- Install and track dependencies
- Create reproducible development environments

### 2. LLM Development & Testing
- Download and test various LLM models
- Compare model performance
- Build applications with local LLM inference
- Integrate with existing applications via OpenAI-compatible API

### 3. RAG Application Development
- Build knowledge bases from documents
- Sync documents from databases and APIs
- Create retrieval systems for chatbots
- Manage vector databases for semantic search

### 4. AI Infrastructure Management
- Centralized management of AI/ML infrastructure
- Resource monitoring and optimization
- Multi-user collaboration with access control
- Audit trail for compliance

---

## 🔒 Security

### Authentication & Authorization
- Session-based authentication
- Role-based access control (RBAC)
- Resource-level permissions
- Audit logging

### Security Best Practices
1. **Change default admin password** immediately
2. **Use strong SECRET_KEY** in production
3. **Enable HTTPS** in production
4. **Encrypt sensitive settings** (database passwords, API keys)
5. **Regular security updates** for dependencies
6. **Monitor audit logs**

---

## 🚢 Deployment

### Development

```bash
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

---

## 🛠️ Troubleshooting

### Common Issues

**Database Connection Errors**
- Check database file permissions (SQLite)
- Verify database connection string
- Ensure database server is running (PostgreSQL, etc.)

**Model Download Failures**
- Check internet connection
- Verify HuggingFace token (if required)
- Check available disk space

**RAG Provider Errors**
- Ensure RAG environment is properly set up
- Check Python dependencies in RAG environment
- Verify data path permissions

**Port Already in Use**
- Change port: `export PORT=5001`
- Kill process using port 5000

For more troubleshooting, see [DOCUMENTATION.md](DOCUMENTATION.md).

---

## 📚 Documentation

- **[Complete Documentation](DOCUMENTATION.md)** - Comprehensive system documentation
- **[Database Documentation](DATABASE.md)** - Database schema and initialization
- **[Migrations README](migrations/README.md)** - Database migration guide

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🙏 Acknowledgments

- **Flask** - Web framework
- **HuggingFace** - Model repository and tools
- **FAISS** - Vector similarity search
- **ChromaDB** - Vector database
- **Bootstrap** - UI framework

---

## 📞 Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Check the [Documentation](DOCUMENTATION.md)
- Review the [Troubleshooting](#-troubleshooting) section

---

**Made with ❤️ for the Python and AI community**

---

**Version**: 1.0.0  
**Last Updated**: 2024
