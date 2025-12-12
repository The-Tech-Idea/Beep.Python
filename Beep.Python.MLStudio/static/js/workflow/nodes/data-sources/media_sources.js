/**
 * Media and Image Data Loading Nodes
 * Load image, audio, and other media files
 */

const MediaSourceNodes = {
    loadImages: {
        type: 'data_load_images',
        name: 'Load Images',
        category: 'data-sources',
        icon: 'bi-image',
        color: '#e91e63',
        description: 'Load images from directory or file paths',
        defaults: {
            path: 'data/images/',
            variable_name: 'images',
            mode: 'directory'
        },
        properties: [
            BaseNode.createProperty('path', 'Path', 'text', {
                required: true,
                placeholder: 'data/images/ or path/to/image.jpg'
            }),
            BaseNode.createProperty('mode', 'Mode', 'select', {
                default: 'directory',
                options: ['directory', 'single_file', 'file_list'],
                help: 'Load from directory, single file, or list of files'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'images'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'images';
            const path = data.path || 'data/images/';
            const mode = data.mode || 'directory';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `from PIL import Image\nimport os\nimport numpy as np\n`;
            if (mode === 'directory') {
                code += `image_files = [os.path.join('${path}', f) for f in os.listdir('${path}') if f.endswith(('.png', '.jpg', '.jpeg', '.bmp', '.gif'))]\n`;
                code += `${varName} = [np.array(Image.open(img)) for img in image_files]\n`;
            } else if (mode === 'single_file') {
                code += `${varName} = np.array(Image.open('${path}'))\n`;
            } else {
                code += `# File list mode - provide comma-separated paths\n`;
                code += `file_list = '${path}'.split(',')\n`;
                code += `${varName} = [np.array(Image.open(f.strip())) for f in file_list]\n`;
            }
            code += `print(f'Loaded {len(${varName}) if isinstance(${varName}, list) else 1} image(s)')\n`;
            
            return code;
        }
    },

    loadImageDataset: {
        type: 'data_load_image_dataset',
        name: 'Load Image Dataset',
        category: 'data-sources',
        icon: 'bi-images',
        color: '#9c27b0',
        description: 'Load image dataset with labels (for classification)',
        defaults: {
            directory: 'data/images/',
            variable_name: 'dataset',
            target_size: '224,224'
        },
        properties: [
            BaseNode.createProperty('directory', 'Directory', 'text', {
                required: true,
                placeholder: 'data/images/',
                help: 'Directory with subdirectories for each class'
            }),
            BaseNode.createProperty('target_size', 'Target Size', 'text', {
                default: '224,224',
                placeholder: '224,224',
                help: 'Width,Height for resizing'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'dataset'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'dataset';
            const directory = data.directory || 'data/images/';
            const targetSize = data.target_size || '224,224';
            const [width, height] = targetSize.split(',').map(s => s.trim());
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `from PIL import Image\nimport os\nimport numpy as np\n`;
            code += `images = []\nlabels = []\n`;
            code += `for class_dir in os.listdir('${directory}'):\n`;
            code += `    class_path = os.path.join('${directory}', class_dir)\n`;
            code += `    if os.path.isdir(class_path):\n`;
            code += `        for img_file in os.listdir(class_path):\n`;
            code += `            if img_file.endswith(('.png', '.jpg', '.jpeg')):\n`;
            code += `                img = Image.open(os.path.join(class_path, img_file))\n`;
            code += `                img = img.resize((${width}, ${height}))\n`;
            code += `                images.append(np.array(img))\n`;
            code += `                labels.append(class_dir)\n`;
            code += `${varName} = {'images': np.array(images), 'labels': labels}\n`;
            code += `print(f'Loaded {len(images)} images with {len(set(labels))} classes')\n`;
            
            return code;
        }
    }
};

// Register all media source nodes
Object.values(MediaSourceNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MediaSourceNodes;
}

