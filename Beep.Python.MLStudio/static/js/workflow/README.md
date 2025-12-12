# Workflow Builder - Modular Architecture

## Overview

The workflow builder uses a modular, extensible architecture with a standard node interface. This allows easy addition of new node types without modifying core code.

## Architecture

```
static/js/workflow/
├── node-registry.js          # Central registry for all nodes
├── nodes/
│   ├── base-node.js          # Standard node interface
│   ├── data-sources.js       # Data loading nodes
│   ├── data-management.js    # Data cleaning/manipulation nodes
│   ├── preprocessing.js       # Preprocessing nodes
│   ├── algorithms.js         # ML algorithm nodes
│   ├── evaluation.js         # Evaluation nodes
│   └── output.js             # Output nodes
├── workflow-builder.js       # Main workflow builder
└── workflow-loader.js        # Module loader (future use)
```

## Standard Node Interface

All nodes must implement this interface:

```javascript
{
    type: 'unique_node_type',        // Required: Unique identifier
    name: 'Display Name',            // Required: Human-readable name
    category: 'category-name',       // Required: Category for grouping
    icon: 'bi-icon-name',            // Required: Bootstrap icon class
    color: '#hexcolor',              // Required: Color code
    description: 'Description',       // Required: Short description
    
    defaults: {                       // Required: Default configuration
        param1: 'default_value',
        param2: 42
    },
    
    properties: [                    // Required: Property definitions
        BaseNode.createProperty('param1', 'Param 1', 'text', {
            required: true,
            help: 'Help text'
        })
    ],
    
    generateCode: (node, context) => {  // Required: Code generator
        // Generate Python code for this node
        return 'generated_code_string';
    },
    
    validate: (node) => {            // Optional: Validation
        return { valid: true, errors: [] };
    }
}
```

## Adding New Node Types

### Step 1: Create Node Definition

Create a new file or add to an existing category file:

```javascript
// static/js/workflow/nodes/my-custom-nodes.js

const MyCustomNodes = {
    myCustomNode: {
        type: 'my_custom_node',
        name: 'My Custom Node',
        category: 'my-category',
        icon: 'bi-star',
        color: '#ff6b6b',
        description: 'Does something custom',
        defaults: {
            param1: 'default'
        },
        properties: [
            BaseNode.createProperty('param1', 'Parameter 1', 'text', {
                required: true,
                help: 'Description of parameter'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            return `# Custom code\nresult = do_something('${data.param1}')`;
        }
    }
};

// Register nodes
Object.values(MyCustomNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});
```

### Step 2: Include in Template

Add the script to `templates/projects/detail.html`:

```html
<script src="{{ url_for('static', filename='js/workflow/nodes/my-custom-nodes.js') }}"></script>
```

### Step 3: That's It!

The node will automatically:
- Appear in the node palette
- Be draggable to canvas
- Show properties panel when selected
- Generate code when workflow is executed

## Property Types

- `text`: Text input
- `number`: Number input (supports min, max, step)
- `boolean`: Checkbox
- `select`: Dropdown (requires options array)
- `array`: Array input (comma-separated)
- `json`: JSON input

## Code Generation Context

The `context` object passed to `generateCode` provides:

- `getInputVariable(node)`: Get the variable name from the input node
- `getOutputVariable(node)`: Get the variable name for this node's output
- `projectFramework`: The project's ML framework
- `workflowData`: Complete workflow data structure

## Categories

Standard categories:
- `data-sources`: Data loading nodes
- `data-management`: Data cleaning/manipulation
- `preprocessing`: Feature preprocessing
- `algorithms-classification`: Classification algorithms
- `algorithms-regression`: Regression algorithms
- `evaluation`: Model evaluation
- `output`: Saving/exporting results

## Extensibility

The system is designed to be extended:

1. **Custom Categories**: Add new categories by using them in node definitions
2. **Custom Properties**: Use `BaseNode.createProperty()` with custom options
3. **Custom Code Generation**: Implement complex logic in `generateCode`
4. **Validation**: Add `validate` function for node-specific validation
5. **Plugin System**: Future support for loading custom nodes from external files

## Best Practices

1. **Follow the Interface**: Always use `BaseNode.validateDefinition()` before registering
2. **Use Defaults**: Provide sensible defaults for all parameters
3. **Helpful Descriptions**: Write clear descriptions and help text
4. **Error Handling**: Handle missing or invalid data in `generateCode`
5. **Consistent Naming**: Use consistent naming conventions for node types

