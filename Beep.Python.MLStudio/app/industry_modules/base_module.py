"""
Base Industry Module
Defines the interface and common functionality for all industry modules
"""

from abc import ABC, abstractmethod
from typing import Dict, List, Any, Optional
from dataclasses import dataclass, field
from enum import Enum


class ModuleCategory(Enum):
    """Categories for industry modules"""
    GENERAL = "general"
    FINANCE = "finance"
    PETROLEUM = "petroleum"
    HEALTHCARE = "healthcare"
    MANUFACTURING = "manufacturing"
    RETAIL = "retail"
    ENERGY = "energy"
    AGRICULTURE = "agriculture"


@dataclass
class NodeDefinition:
    """Definition for a custom node"""
    id: str
    name: str
    category: str
    description: str
    icon: str
    color: str
    inputs: List[Dict[str, str]]
    outputs: List[Dict[str, str]]
    properties: List[Dict[str, Any]]
    code_template: str
    imports: List[str] = field(default_factory=list)


@dataclass
class WorkflowTemplate:
    """Definition for a workflow template"""
    id: str
    name: str
    description: str
    category: str
    icon: str
    color: str
    tags: List[str]
    nodes: List[Dict[str, Any]]
    edges: List[Dict[str, Any]]
    difficulty: str = "intermediate"  # beginner, intermediate, advanced


@dataclass
class SampleDataset:
    """Definition for a sample dataset"""
    id: str
    name: str
    description: str
    filename: str
    format: str  # csv, json, excel, parquet
    columns: List[Dict[str, str]]
    rows: int
    size_kb: int
    url: Optional[str] = None  # For downloadable datasets


class IndustryModule(ABC):
    """
    Base class for all industry modules.
    Each module provides specialized functionality for a specific industry.
    """
    
    def __init__(self):
        self._nodes: Dict[str, NodeDefinition] = {}
        self._templates: Dict[str, WorkflowTemplate] = {}
        self._datasets: Dict[str, SampleDataset] = {}
        self._initialize()
    
    @property
    @abstractmethod
    def id(self) -> str:
        """Unique identifier for this module"""
        pass
    
    @property
    @abstractmethod
    def name(self) -> str:
        """Display name for this module"""
        pass
    
    @property
    @abstractmethod
    def description(self) -> str:
        """Description of what this module provides"""
        pass
    
    @property
    @abstractmethod
    def category(self) -> ModuleCategory:
        """Category this module belongs to"""
        pass
    
    @property
    @abstractmethod
    def version(self) -> str:
        """Version of this module"""
        pass
    
    @property
    def icon(self) -> str:
        """Bootstrap icon class for this module"""
        return "bi-box"
    
    @property
    def color(self) -> str:
        """Theme color for this module"""
        return "#6c757d"
    
    @abstractmethod
    def _initialize(self):
        """Initialize module-specific nodes, templates, and datasets"""
        pass
    
    # ============== Nodes ==============
    
    def register_node(self, node: NodeDefinition):
        """Register a custom node"""
        self._nodes[node.id] = node
    
    def get_nodes(self) -> Dict[str, NodeDefinition]:
        """Get all custom nodes for this module"""
        return self._nodes.copy()
    
    def get_node(self, node_id: str) -> Optional[NodeDefinition]:
        """Get a specific node by ID"""
        return self._nodes.get(node_id)
    
    # ============== Templates ==============
    
    def register_template(self, template: WorkflowTemplate):
        """Register a workflow template"""
        self._templates[template.id] = template
    
    def get_templates(self) -> Dict[str, WorkflowTemplate]:
        """Get all workflow templates for this module"""
        return self._templates.copy()
    
    def get_template(self, template_id: str) -> Optional[WorkflowTemplate]:
        """Get a specific template by ID"""
        return self._templates.get(template_id)
    
    # ============== Datasets ==============
    
    def register_dataset(self, dataset: SampleDataset):
        """Register a sample dataset"""
        self._datasets[dataset.id] = dataset
    
    def get_datasets(self) -> Dict[str, SampleDataset]:
        """Get all sample datasets for this module"""
        return self._datasets.copy()
    
    def get_dataset(self, dataset_id: str) -> Optional[SampleDataset]:
        """Get a specific dataset by ID"""
        return self._datasets.get(dataset_id)
    
    # ============== Serialization ==============
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize module to dictionary"""
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'category': self.category.value,
            'version': self.version,
            'icon': self.icon,
            'color': self.color,
            'nodes': [self._node_to_dict(n) for n in self._nodes.values()],
            'templates': [self._template_to_dict(t) for t in self._templates.values()],
            'datasets': [self._dataset_to_dict(d) for d in self._datasets.values()],
        }
    
    def _node_to_dict(self, node: NodeDefinition) -> Dict[str, Any]:
        return {
            'id': node.id,
            'name': node.name,
            'category': node.category,
            'description': node.description,
            'icon': node.icon,
            'color': node.color,
            'inputs': node.inputs,
            'outputs': node.outputs,
            'properties': node.properties,
            'code_template': node.code_template,
            'imports': node.imports,
        }
    
    def _template_to_dict(self, template: WorkflowTemplate) -> Dict[str, Any]:
        return {
            'id': template.id,
            'name': template.name,
            'description': template.description,
            'category': template.category,
            'icon': template.icon,
            'color': template.color,
            'tags': template.tags,
            'difficulty': template.difficulty,
            'workflow': {
                'nodes': template.nodes,
                'edges': template.edges,
            }
        }
    
    def _dataset_to_dict(self, dataset: SampleDataset) -> Dict[str, Any]:
        return {
            'id': dataset.id,
            'name': dataset.name,
            'description': dataset.description,
            'filename': dataset.filename,
            'format': dataset.format,
            'columns': dataset.columns,
            'rows': dataset.rows,
            'size_kb': dataset.size_kb,
            'url': dataset.url,
        }


class ModuleRegistry:
    """Registry for all industry modules"""
    
    def __init__(self):
        self._modules: Dict[str, IndustryModule] = {}
    
    def register(self, module: IndustryModule):
        """Register an industry module"""
        self._modules[module.id] = module
        print(f"✓ Registered industry module: {module.name} ({module.id})")
    
    def unregister(self, module_id: str):
        """Unregister an industry module"""
        if module_id in self._modules:
            del self._modules[module_id]
    
    def get_module(self, module_id: str) -> Optional[IndustryModule]:
        """Get a module by ID"""
        return self._modules.get(module_id)
    
    def get_all_modules(self) -> Dict[str, IndustryModule]:
        """Get all registered modules"""
        return self._modules.copy()
    
    def get_modules_by_category(self, category: ModuleCategory) -> List[IndustryModule]:
        """Get all modules in a category"""
        return [m for m in self._modules.values() if m.category == category]
    
    def get_all_nodes(self) -> Dict[str, NodeDefinition]:
        """Get all nodes from all modules"""
        all_nodes = {}
        for module in self._modules.values():
            for node_id, node in module.get_nodes().items():
                all_nodes[f"{module.id}_{node_id}"] = node
        return all_nodes
    
    def get_all_templates(self) -> Dict[str, WorkflowTemplate]:
        """Get all templates from all modules"""
        all_templates = {}
        for module in self._modules.values():
            for template_id, template in module.get_templates().items():
                all_templates[f"{module.id}_{template_id}"] = template
        return all_templates
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize registry to dictionary"""
        return {
            'modules': {mid: m.to_dict() for mid, m in self._modules.items()}
        }

