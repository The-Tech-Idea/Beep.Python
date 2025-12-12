# Visual Workflow/Pipeline Builder Guide

## Overview

MLStudio now includes a powerful visual workflow builder that allows you to create ML pipelines by dragging and dropping nodes, similar to professional tools like KNIME, RapidMiner, or Azure ML Designer.

## Features

### 🎨 Visual Pipeline Builder
- **Drag-and-Drop Interface**: Simply drag nodes from the palette onto the canvas
- **Node Connections**: Connect nodes to define data flow
- **Node Library**: Pre-built nodes for common ML operations
- **Save & Load**: Save workflows and load them later
- **Code Generation**: Automatically convert visual workflows to Python code
- **Direct Execution**: Run workflows directly from the visual editor

### 📦 Node Library

#### Data Sources
- **Load CSV**: Load data from CSV files
- **Load JSON**: Load data from JSON files

#### Preprocessing
- **Train/Test Split**: Split data into training and testing sets
- **Scale Features**: Standardize/normalize features
- **Label Encode**: Encode categorical labels

#### Models
- **Classifier**: Train classification models (Random Forest, etc.)
- **Regressor**: Train regression models

#### Evaluation
- **Calculate Metrics**: Compute accuracy, MSE, R², etc.

#### Output
- **Save Model**: Save trained models to disk

## How to Use

### 1. Access the Workflow Builder

1. Open any project
2. Click on the **"Visual Pipeline"** tab
3. The workflow builder will open with:
   - **Node Library** on the left (draggable nodes)
   - **Canvas** in the center (where you build your pipeline)
   - **Generated Code** panel at the bottom

### 2. Create a Workflow

1. **Add Nodes**: Drag nodes from the Node Library onto the canvas
2. **Connect Nodes**: 
   - Click and drag from the output port (red) of one node
   - Connect it to the input port (green) of another node
3. **Arrange Nodes**: Drag nodes to organize your pipeline visually
4. **Delete Nodes**: Click the X button on a node to remove it

### 3. Save Your Workflow

1. Click **"Save Workflow"** button
2. Enter a name for your workflow
3. The workflow is saved and can be loaded later

### 4. Generate Python Code

1. Build your pipeline with nodes
2. Click **"Generate Code"** button
3. The generated Python code will appear in the code panel
4. You can:
   - **Copy** the code to clipboard
   - **Save as Script** to save it as a Python file
   - **Edit** the code manually if needed

### 5. Execute Workflow

1. Save your workflow first
2. Click **"Execute"** button
3. The workflow will run and create an experiment
4. Check the **Experiments** tab for results

## Workflow Example

### Simple Classification Pipeline

1. **Load CSV** → Load your dataset
2. **Train/Test Split** → Split data (connect from Load CSV)
3. **Scale Features** → Normalize features (connect from Split)
4. **Classifier** → Train model (connect from Scale)
5. **Evaluate** → Calculate metrics (connect from Classifier)
6. **Save Model** → Save trained model (connect from Classifier)

## Node Configuration

Currently, nodes use default configurations. Future versions will include:
- Node property panels
- Customizable parameters
- Data preview
- Validation

## Best Practices

1. **Start Simple**: Begin with basic pipelines and add complexity
2. **Save Frequently**: Save your work regularly
3. **Test Incrementally**: Generate code and test after adding nodes
4. **Organize Visually**: Arrange nodes for clarity
5. **Use Templates**: Start from saved workflows as templates

## Workflow Storage

- Workflows are stored in the database
- Each project can have multiple workflows
- Workflows include:
  - Node positions
  - Connections
  - Node configurations
  - Generated code

## API Endpoints

### List Workflows
```
GET /api/v1/projects/{project_id}/workflows
```

### Create Workflow
```
POST /api/v1/projects/{project_id}/workflows
Body: { name, description, workflow_data }
```

### Get Workflow
```
GET /api/v1/workflows/{workflow_id}
```

### Update Workflow
```
PUT /api/v1/workflows/{workflow_id}
Body: { name, description, workflow_data, status }
```

### Delete Workflow
```
DELETE /api/v1/workflows/{workflow_id}
```

### Generate Code
```
POST /api/v1/workflows/{workflow_id}/generate-code
Body: { workflow_data }
```

### Execute Workflow
```
POST /api/v1/workflows/{workflow_id}/execute
```

## Technical Details

### Workflow Data Structure

```json
{
  "nodes": [
    {
      "id": "node_1",
      "type": "data_load_csv",
      "position": { "x": 100, "y": 100 },
      "data": { "file_path": "data/dataset.csv" }
    }
  ],
  "edges": [
    {
      "id": "edge_1",
      "source": "node_1",
      "target": "node_2"
    }
  ],
  "viewport": { "x": 0, "y": 0, "zoom": 1 }
}
```

### Code Generation

The workflow service analyzes:
- Node types and order
- Connections between nodes
- Node configurations
- Project framework (scikit-learn, TensorFlow, etc.)

And generates optimized Python code that:
- Imports required libraries
- Loads and preprocesses data
- Trains models
- Evaluates performance
- Saves models

## Future Enhancements

Planned features:
- [ ] Node property editors
- [ ] Data preview in nodes
- [ ] More node types (feature engineering, hyperparameter tuning)
- [ ] Workflow templates
- [ ] Workflow versioning
- [ ] Collaborative editing
- [ ] Export/import workflows
- [ ] Visual debugging
- [ ] Performance profiling

## Troubleshooting

### Nodes not connecting?
- Make sure you drag from output port (red) to input port (green)
- Check that nodes are properly positioned

### Code not generating?
- Ensure nodes are connected in a valid flow
- Check that you have at least one data source node

### Workflow not executing?
- Save the workflow first
- Generate code before executing
- Check that all required nodes are present

## Support

For issues or questions:
1. Check the generated code for errors
2. Review node connections
3. Ensure data files exist
4. Check experiment logs in the Experiments tab

---

**Enjoy building ML pipelines visually!** 🚀

