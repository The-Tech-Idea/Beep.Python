# Quick Migration Guide

## Running the Migration

### Option 1: Command Line
```bash
python app/migrations/migrate_environments_and_middleware.py
```

### Option 2: Python Script
```python
from app import create_app
from app.migrations.migrate_environments_and_middleware import main

app = create_app()
with app.app_context():
    main()
```

## What the Migration Does

1. ✅ Updates environment paths in settings to `providers/` directory
2. ✅ Creates database tables: `middleware_routing_rules` and `middleware_access_policies`
3. ✅ Migrates existing rules/policies from Settings JSON to new tables
4. ✅ Updates RAG provider paths to use `providers/rag`

## After Migration

### Old Directories - Safe to Delete

After verifying everything works (1-2 days), you can delete:
- `{app_dir}/doc_extraction_env/`
- `{app_dir}/rag_env/`
- `{app_dir}/rag_data/venv/`
- `{app_dir}/data/ai_services/*/venv/`

**Impact:** None - app will use new environments in `providers/`

### Reinstallation

If you delete old directories:
- Environments will auto-create in `providers/` when you click "Create Environment"
- You'll need to reinstall packages (click "Install Packages" in each service)

## Verification

1. Check `{app_dir}/providers/` exists with subdirectories
2. Check database has `middleware_routing_rules` and `middleware_access_policies` tables
3. Test creating a rule/policy in the UI
4. Verify it appears in the database

## Rollback

If needed:
- Old JSON data still in `Setting` table (not deleted)
- Drop new tables: `DROP TABLE middleware_routing_rules; DROP TABLE middleware_access_policies;`
- Code falls back to Settings JSON automatically
