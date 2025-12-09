# RBAC Migration

This directory contains database migration scripts for the RBAC (Role-Based Access Control) system.

## Running the Migration

To add RBAC support to your database:

```bash
python migrations/rbac_migration.py
```

This will:
1. Create new tables: `roles`, `groups`, `user_groups`
2. Add `role_id` column to `users` table
3. Create 4 default roles (Admin, Power User, User, Guest)
4. Assign roles to existing users

## Rollback (Use with Caution!)

To remove RBAC tables and data:

```bash
python migrations/rbac_migration.py --rollback
```

⚠️ **WARNING**: This will delete all roles, groups, and role assignments!

## Default Roles

After migration, the following roles are available:

| Role | Permissions | Description |
|------|-------------|-------------|
| **Admin** | All permissions | Full system access |
| **Power User** | LLM (view, load, manage_env), RAG (read, write) | Can create and manage own resources |
| **User** | LLM (view, load), RAG (read) | Standard user access |
| **Guest** | LLM (view), RAG (read) | Read-only access |

## Permissions

Available permissions:
- `llm:view` - View LLM models
- `llm:load` - Load LLM models for inference
- `llm:delete` - Delete LLM models
- `llm:manage_env` - Manage virtual environments
- `rag:read` - Read RAG collections
- `rag:write` - Write to RAG collections
- `rag:admin` - Administer RAG collections
- `system:admin` - System administration
- `user:manage` - Manage users
- `group:manage` - Manage groups

## Troubleshooting

If migration fails:
1. Check database connection string
2. Ensure you have write permissions
3. Check for existing table conflicts
4. Review error messages in console output
