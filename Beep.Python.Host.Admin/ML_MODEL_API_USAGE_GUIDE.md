# ML Model API Usage Guide

## Overview

Users can now upload their ML models and use them through RESTful APIs. The system provides:
- ✅ Model upload via API or Web UI
- ✅ Automatic validation
- ✅ RESTful API endpoints for predictions
- ✅ Generated API endpoints with OpenAPI documentation
- ✅ Usage analytics and logging
- ✅ Permission management

## Quick Start

### 1. Upload a Model

#### Via API (multipart/form-data):
```bash
curl -X POST http://localhost:5000/api/v1/ml-models \
  -H "Cookie: session=YOUR_SESSION_COOKIE" \
  -F "file=@my_model.pkl" \
  -F "name=My ML Model" \
  -F "description=Classification model" \
  -F "model_type=sklearn" \
  -F "category=classification" \
  -F "is_public=false"
```

#### Response:
```json
{
  "success": true,
  "model_id": "abc123",
  "version_id": "v1",
  "validation_id": "val_xyz",
  "status": "pending_validation",
  "message": "Model uploaded successfully"
}
```

### 2. Check Validation Status

```bash
curl -X GET http://localhost:5000/api/v1/ml-models/{model_id}/validations/{validation_id} \
  -H "Cookie: session=YOUR_SESSION_COOKIE"
```

### 3. Make Predictions

#### Single Prediction:
```bash
curl -X POST http://localhost:5000/api/v1/ml-models/{model_id}/predict \
  -H "Cookie: session=YOUR_SESSION_COOKIE" \
  -H "Content-Type: application/json" \
  -d '{
    "data": [1.0, 2.0, 3.0, 4.0]
  }'
```

#### Batch Predictions:
```bash
curl -X POST http://localhost:5000/api/v1/ml-models/{model_id}/predict/batch \
  -H "Cookie: session=YOUR_SESSION_COOKIE" \
  -H "Content-Type: application/json" \
  -d '{
    "data": [
      [1.0, 2.0, 3.0, 4.0],
      [5.0, 6.0, 7.0, 8.0]
    ]
  }'
```

#### Response:
```json
{
  "success": true,
  "prediction": [0.85, 0.15],
  "model_id": "abc123",
  "version": "v1",
  "response_time_ms": 45
}
```

### 4. Generate Custom API Endpoint

```bash
curl -X POST http://localhost:5000/api/v1/ml-models/{model_id}/generate-api \
  -H "Cookie: session=YOUR_SESSION_COOKIE" \
  -H "Content-Type: application/json" \
  -d '{
    "endpoint_path": "/api/v1/my-custom-model",
    "description": "Custom endpoint for my model"
  }'
```

#### Response:
```json
{
  "success": true,
  "api_id": "api_xyz",
  "endpoint_path": "/api/v1/my-custom-model",
  "openapi_spec": { ... }
}
```

### 5. Use Generated API Endpoint

```bash
curl -X POST http://localhost:5000/api/v1/my-custom-model \
  -H "Cookie: session=YOUR_SESSION_COOKIE" \
  -H "Content-Type: application/json" \
  -d '{
    "data": [1.0, 2.0, 3.0, 4.0]
  }'
```

## API Endpoints Reference

### Model Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/ml-models` | Upload a new model |
| `GET` | `/api/v1/ml-models` | List all models (with filters) |
| `GET` | `/api/v1/ml-models/{model_id}` | Get model details |
| `PATCH` | `/api/v1/ml-models/{model_id}` | Update model metadata |
| `DELETE` | `/api/v1/ml-models/{model_id}` | Delete a model |

### Validation

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/ml-models/{model_id}/validate` | Start validation |
| `GET` | `/api/v1/ml-models/{model_id}/validations/{validation_id}` | Get validation status |

### Predictions

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/ml-models/{model_id}/predict` | Single prediction |
| `POST` | `/api/v1/ml-models/{model_id}/predict/batch` | Batch predictions |

### API Generation

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/ml-models/{model_id}/generate-api` | Generate custom API endpoint |
| `GET` | `/api/v1/ml-models/{model_id}/docs` | Get OpenAPI documentation |

## Authentication

All endpoints require authentication via session cookie. Users must:
1. Login via `/login`
2. Use the session cookie in subsequent requests
3. Have appropriate permissions (read/execute/admin)

## Model Types Supported

- **scikit-learn**: `.pkl`, `.joblib`
- **TensorFlow**: `.h5`, `.pb`, SavedModel
- **PyTorch**: `.pt`, `.pth`
- **XGBoost**: `.pkl`, `.json`
- **ONNX**: `.onnx`
- **Generic**: Pickle/Joblib files

## Input Format

### Single Prediction
```json
{
  "data": [1.0, 2.0, 3.0, 4.0]
}
```

Or with named features:
```json
{
  "data": {
    "feature1": 1.0,
    "feature2": 2.0,
    "feature3": 3.0,
    "feature4": 4.0
  }
}
```

### Batch Prediction
```json
{
  "data": [
    [1.0, 2.0, 3.0, 4.0],
    [5.0, 6.0, 7.0, 8.0],
    [9.0, 10.0, 11.0, 12.0]
  ]
}
```

## Response Format

### Success Response
```json
{
  "success": true,
  "prediction": [0.85, 0.15],
  "model_id": "abc123",
  "version": "v1",
  "response_time_ms": 45
}
```

### Error Response
```json
{
  "success": false,
  "error": "Model file not found",
  "model_id": "abc123"
}
```

## Environment Requirements

**Important**: The ML environment must be set up before models can be used. The system will:
1. Auto-create the environment if it doesn't exist
2. Auto-install required packages
3. Run all predictions in isolated environment

To manually set up:
```bash
# Via UI: Settings → ML Models → Environment Setup
# Or via API (admin only):
POST /api/v1/ml-models/settings/environment/setup
```

## Usage Analytics

All API calls are logged with:
- Timestamp
- User ID
- Input data (anonymized)
- Response status
- Response time
- Error messages (if any)

View usage stats:
```bash
GET /api/v1/ml-models/{model_id}
# Returns usage_stats in response
```

## Permissions

- **Owner**: Full access (read, execute, update, delete)
- **Public Models**: Anyone can read/execute
- **Private Models**: Only owner and admins can access
- **Admin**: Full access to all models

## Example: Complete Workflow

### 1. Upload Model
```bash
curl -X POST http://localhost:5000/api/v1/ml-models \
  -H "Cookie: session=..." \
  -F "file=@model.pkl" \
  -F "name=MyModel" \
  -F "model_type=sklearn"
```

### 2. Wait for Validation
```bash
# Check validation status
curl http://localhost:5000/api/v1/ml-models/{model_id}/validations/{validation_id} \
  -H "Cookie: session=..."
```

### 3. Make Prediction
```bash
curl -X POST http://localhost:5000/api/v1/ml-models/{model_id}/predict \
  -H "Cookie: session=..." \
  -H "Content-Type: application/json" \
  -d '{"data": [1.0, 2.0, 3.0]}'
```

### 4. Generate Custom API
```bash
curl -X POST http://localhost:5000/api/v1/ml-models/{model_id}/generate-api \
  -H "Cookie: session=..." \
  -H "Content-Type: application/json" \
  -d '{"endpoint_path": "/api/v1/my-model"}'
```

### 5. Use Custom Endpoint
```bash
curl -X POST http://localhost:5000/api/v1/my-model \
  -H "Cookie: session=..." \
  -H "Content-Type: application/json" \
  -d '{"data": [1.0, 2.0, 3.0]}'
```

## Web UI

Users can also:
- Upload models via web interface: `/ml-models/upload`
- Browse models: `/ml-models`
- View model details: `/ml-models/{model_id}`
- Browse marketplace: `/ml-models/marketplace`
- Manage settings: `/ml-models/settings`

## Error Handling

Common errors and solutions:

| Error | Solution |
|-------|----------|
| `ML environment not ready` | Set up environment via Settings |
| `Model file not found` | Re-upload the model |
| `Permission denied` | Check model visibility and user permissions |
| `Validation failed` | Check model file and requirements |
| `Prediction failed` | Check input format and model compatibility |

## Best Practices

1. **Always validate models** before using in production
2. **Use batch predictions** for multiple inputs (more efficient)
3. **Monitor usage stats** to track performance
4. **Set appropriate permissions** (public vs private)
5. **Include requirements.txt** for model dependencies
6. **Document your models** with descriptions and tags

---

**Ready to use!** Users can now upload ML models and access them via RESTful APIs with full authentication, validation, and analytics.

