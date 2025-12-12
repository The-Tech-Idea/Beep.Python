# Settings System Guide

## Overview

MLStudio now includes a comprehensive centralized settings system that manages all configuration, paths, and preferences. All services (Environment Manager, ML Service, Data Service, etc.) use this centralized system.

## Features

### ✅ Centralized Configuration
- **Single Source of Truth**: All settings stored in database
- **Type-Safe**: Supports string, number, boolean, JSON, and path types
- **Categorized**: Settings organized by category (general, paths, environment, ml, ui)
- **Cached**: Settings are cached for performance
- **Default Values**: Automatic initialization with sensible defaults

### ✅ Settings Categories

#### General Settings
- Application name and version
- Debug mode
- Log level

#### Path Settings
- Base path
- Projects folder
- Data folder
- Models folder
- Providers (environments) folder
- Embedded Python path

#### Environment Settings
- Environment manager type (local/host_admin)
- Host Admin URL and API key
- Default Python version
- Auto-create environments

#### ML Settings
- Default framework
- Max upload size
- Default train/test split
- Default random state
- Auto-save models

#### UI Settings
- Theme (light/dark)
- Items per page
- Auto-refresh interval
- Show advanced options

## Usage

### Accessing Settings

#### In Python Code

```python
from app.services.settings_manager import get_settings_manager

settings_mgr = get_settings_manager()

# Get a setting
projects_folder = settings_mgr.get('projects_folder', 'projects')
max_upload = settings_mgr.get_max_upload_size()

# Get all settings in a category
path_settings = settings_mgr.get_by_category('paths')

# Set a setting
settings_mgr.set('max_upload_size_mb', 200)
```

#### Convenience Methods

```python
# Get paths (returns Path objects)
projects_path = settings_mgr.get_projects_folder()
data_path = settings_mgr.get_data_folder()
models_path = settings_mgr.get_models_folder()
providers_path = settings_mgr.get_providers_folder()
python_embedded_path = settings_mgr.get_python_embedded_path()

# Get other common settings
host_admin_url = settings_mgr.get_host_admin_url()
max_upload_bytes = settings_mgr.get_max_upload_size()
is_debug = settings_mgr.is_debug_mode()
default_framework = settings_mgr.get_default_framework()
```

### Settings UI

Access the settings page:
1. Click **Settings** in the navigation bar
2. Browse settings by category
3. Edit values directly in the form
4. Click **Save All Settings** to apply changes

### API Endpoints

#### List Settings
```
GET /api/v1/settings
GET /api/v1/settings?category=paths
```

#### Get Setting
```
GET /api/v1/settings/{key}
```

#### Update Setting
```
PUT /api/v1/settings/{key}
Body: { "value": "new_value" }
```

#### Create Setting
```
POST /api/v1/settings
Body: {
  "key": "custom_setting",
  "category": "general",
  "value_type": "string",
  "value": "value",
  "description": "Description"
}
```

#### Delete Setting
```
DELETE /api/v1/settings/{key}
```

#### Reset Settings
```
POST /api/v1/settings/reset
Body: { "category": "paths" }  # Optional category
```

## Service Integration

All services now use centralized settings:

### Environment Manager
- Uses `providers_folder` from settings
- Uses `python_embedded_path` from settings
- Uses `base_path` from settings

### ML Service
- Uses `projects_folder` from settings
- Automatically creates EnvironmentManager with settings

### Data Service
- Uses `data_folder` from settings
- Uses `max_upload_size_mb` from settings

### Application Initialization
- Uses settings for upload folder
- Uses settings for projects folder
- Uses settings for max upload size

## Default Settings

Settings are automatically initialized with defaults on first run:

```python
# Paths
projects_folder: 'projects'
data_folder: 'data'
models_folder: 'models'
providers_folder: 'providers'
python_embedded_path: 'python-embedded'

# Environment
environment_manager_type: 'local'
host_admin_url: 'http://127.0.0.1:5000'
default_python_version: '3.11'
auto_create_environments: true

# ML
default_framework: 'scikit-learn'
max_upload_size_mb: 100
default_train_test_split: 0.2
default_random_state: 42
auto_save_models: true

# UI
theme: 'light'
items_per_page: 20
auto_refresh_interval: 30
show_advanced_options: false
```

## Best Practices

1. **Use Settings Manager**: Always use `get_settings_manager()` instead of hardcoding values
2. **Check Settings First**: Services should check settings before using defaults
3. **Path Resolution**: Settings manager handles relative/absolute path resolution
4. **Cache Awareness**: Settings are cached - use `_settings_cache.clear()` after updates
5. **Type Safety**: Use appropriate value types (boolean, number, etc.)

## Migration

Existing code using hardcoded paths or environment variables will continue to work, but should be migrated to use settings:

### Before
```python
upload_folder = os.environ.get('UPLOAD_FOLDER', 'data')
projects_folder = os.environ.get('PROJECTS_FOLDER', 'projects')
```

### After
```python
from app.services.settings_manager import get_settings_manager
settings_mgr = get_settings_manager()
upload_folder = settings_mgr.get_data_folder()
projects_folder = settings_mgr.get_projects_folder()
```

## Troubleshooting

### Settings Not Saving
- Check database connection
- Verify settings table exists
- Check for validation errors

### Paths Not Working
- Ensure paths are correctly set in settings
- Check that directories exist or can be created
- Verify path permissions

### Services Not Using Settings
- Ensure services are using `get_settings_manager()`
- Check that settings are initialized
- Verify service initialization order

## Future Enhancements

- [ ] Settings validation rules
- [ ] Settings import/export
- [ ] Settings versioning
- [ ] Environment-specific settings
- [ ] Settings templates
- [ ] Settings search/filter
- [ ] Settings history/audit log

---

**All configuration is now centralized and manageable through the Settings page!** ⚙️

