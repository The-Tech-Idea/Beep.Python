# Database Initialization

## Quick Start

### Initialize Complete Database:

```bash
python init_database.py
```

This creates:
- ✅ All database tables
- ✅ Default roles (Admin, Power User, User, Guest)
- ✅ Admin user (username: `admin`, password: `admin123`)
- ✅ Default group
- ✅ Default settings

### View Schema:

```bash
python init_database.py --schema
```

Shows all tables and columns.

---

## What Gets Created

### Tables:
1. **users** - User accounts
2. **roles** - User roles (RBAC)
3. **groups** - User groups
4. **user_groups** - User-group associations
5. **settings** - Application settings
6. **audit_log** - Audit trail
7. **collections** - RAG collections
8. **documents** - RAG documents
9. **access_privileges** - Resource permissions

### Default Data:
- **4 Roles**: Admin, Power User, User, Guest
- **1 User**: admin / admin123
- **1 Group**: Default
- **4 Settings**: App name, version, limits

---

## Usage

```bash
# First time setup
python init_database.py

# Reset database (WARNING: Deletes all data!)
python init_database.py --init

# View schema
python init_database.py --schema
```

---

## After Initialization

1. Start application: `python run.py`
2. Login with: `admin` / `admin123`
3. **Change admin password immediately!**
