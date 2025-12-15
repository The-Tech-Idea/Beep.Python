# Migration Guide: Virtual Environments & Middleware Database

## Overview

This migration:
1. **Moves virtual environments to centralized `providers/` directory**
2. **Creates proper database tables for middleware rules and policies**
3. **Migrates existing data from Settings JSON to new tables**

## Running the Migration

### Step 1: Run the Migration Script

```bash
python app/migrations/migrate_environments_and_middleware.py
```

Or from Python:
```python
from app.migrations.migrate_environments_and_middleware import main
main()
```

### Step 2: Verify Migration

1. Check that new environments are in `{app_dir}/providers/`:
   - `providers/document_extraction`
   - `providers/job_scheduler`
   - `providers/rag`
   - `providers/ai_services_{service_type}`

2. Verify database tables exist:
   - `middleware_routing_rules`
   - `middleware_access_policies`

3. Check that rules/policies were migrated:
   - Query the new tables to verify data

## What Happens to Old Directories?

### Safe to Delete (After Verification)

After verifying the new environments work correctly, you can safely delete:

- `{app_dir}/doc_extraction_env/` (if exists)
- `{app_dir}/rag_env/` (if exists)
- `{app_dir}/rag_data/venv/` (if exists)
- `{app_dir}/data/ai_services/*/venv/` (if exists)

### Impact of Deleting Old Directories

**If you delete old directories:**
- ✅ **No impact** - The app will automatically use the new environments in `providers/`
- ✅ **Settings updated** - All environment paths in settings are updated to point to `providers/`
- ✅ **Code updated** - All code references updated to use `providers/`

**If you keep old directories:**
- ⚠️ **Waste of disk space** - Old environments are no longer used
- ⚠️ **Confusion** - Multiple environment directories can be confusing

**Recommendation:** Delete old directories after verifying everything works (about 1-2 days of testing).

## Reinstallation

If you delete old directories and need to reinstall:

1. **No manual reinstallation needed** - The app will automatically:
   - Detect missing environments
   - Create new ones in `providers/` when you click "Create Environment"
   - Install packages as needed

2. **Package reinstallation:**
   - Go to each service's environment page
   - Click "Install Packages" or "Install All"
   - Packages will be installed in the new `providers/` location

## Database Changes

### New Tables Created

1. **`middleware_routing_rules`**
   - Stores routing rules with proper indexing
   - Better query performance
   - Easier to manage via SQL

2. **`middleware_access_policies`**
   - Stores access policies with proper indexing
   - Better query performance
   - Easier to manage via SQL

### Data Migration

- Existing rules/policies in `Setting` table (JSON) are migrated to new tables
- Old JSON data is kept as backup (not deleted)
- New rules/policies are saved to database tables
- Code falls back to Settings JSON if tables don't exist (backward compatibility)

## Rollback

If you need to rollback:

1. **Environments:** Just recreate them - no data loss
2. **Rules/Policies:** Old JSON data still exists in `Setting` table
3. **Database:** Drop the new tables if needed:
   ```sql
   DROP TABLE IF EXISTS middleware_routing_rules;
   DROP TABLE IF EXISTS middleware_access_policies;
   ```

## Files Updated

### Code Changes
- `app/services/document_extraction_environment.py` - Uses `providers/document_extraction`
- `app/services/ai_services_environment.py` - Uses `providers/ai_services_{type}`
- `app/services/job_scheduler_environment.py` - Uses `providers/job_scheduler`
- `app/services/rag_environment.py` - Uses `providers/rag`
- `app/services/rag_environment_manager.py` - Uses `providers/rag`
- `app/services/rag_providers/*.py` - Updated to use `providers/rag`
- `app/services/middleware_server_manager.py` - Uses database tables (with Settings fallback)

### New Files
- `app/models/middleware.py` - Database models for rules and policies
- `app/migrations/migrate_environments_and_middleware.py` - Migration script

## Troubleshooting

### Issue: "Environment not found"
**Solution:** Create the environment through the UI - it will be created in `providers/`

### Issue: "Tables don't exist"
**Solution:** Run the migration script again, or manually create tables using SQL

### Issue: "Rules/Policies not loading"
**Solution:** Check if tables exist, verify data was migrated, check logs for errors

### Issue: "Old environment still being used"
**Solution:** 
1. Check settings: `Setting.get('doc_extraction_env_path')` etc.
2. Update settings to point to `providers/` paths
3. Restart the application

## Next Steps

1. ✅ Run migration script
2. ✅ Verify new environments work
3. ✅ Test middleware rules and policies
4. ⏳ Wait 1-2 days for stability
5. ⏳ Delete old environment directories
6. ⏳ Optional: Clean up old Settings JSON entries
