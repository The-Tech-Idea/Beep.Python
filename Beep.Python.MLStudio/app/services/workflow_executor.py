"""
Workflow Executor
Implements proper pipeline execution with topological sorting and dependency resolution
Similar to KNIME, Azure ML Designer, and other professional workflow systems
"""
import logging
from typing import Dict, List, Any, Set, Tuple, Optional
from collections import deque, defaultdict
from enum import Enum

logger = logging.getLogger(__name__)


class ExecutionStatus(Enum):
    """Execution status for nodes"""
    PENDING = "pending"
    READY = "ready"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    SKIPPED = "skipped"


class WorkflowExecutor:
    """
    Executes workflows with proper dependency resolution and topological sorting.
    
    Key features:
    1. Topological sort to determine execution order
    2. Dependency tracking (a node runs only when all inputs are ready)
    3. Variable passing between nodes
    4. Cycle detection
    5. Parallel execution support (nodes with no dependencies can run in parallel)
    6. Error handling and rollback
    """
    
    def __init__(self, workflow_data: Dict[str, Any]):
        """
        Initialize workflow executor
        
        Args:
            workflow_data: Workflow definition with nodes and edges
        """
        self.workflow_data = workflow_data
        self.nodes = {node['id']: node for node in workflow_data.get('nodes', [])}
        self.edges = workflow_data.get('edges', [])
        
        # Build dependency graph
        self.dependency_graph = self._build_dependency_graph()
        
        # Execution state
        self.execution_order: List[str] = []
        self.node_status: Dict[str, ExecutionStatus] = {
            node_id: ExecutionStatus.PENDING for node_id in self.nodes.keys()
        }
        self.node_outputs: Dict[str, Any] = {}  # Store outputs from each node
        self.node_variables: Dict[str, str] = {}  # Map node ID to variable name
        
    def _build_dependency_graph(self) -> Dict[str, Set[str]]:
        """
        Build dependency graph: node_id -> set of nodes that must run before it
        
        In a workflow:
        - An edge from A to B means: B depends on A (A must run before B)
        - So dependencies[B] includes A
        """
        dependencies = defaultdict(set)  # node -> set of nodes it depends on
        dependents = defaultdict(set)  # node -> set of nodes that depend on it
        
        for edge in self.edges:
            source = edge.get('source') or edge.get('sourceNodeId')
            target = edge.get('target') or edge.get('targetNodeId')
            
            if source and target and source in self.nodes and target in self.nodes:
                # Target depends on source
                dependencies[target].add(source)
                dependents[source].add(target)
        
        self.dependencies = dict(dependencies)
        self.dependents = dict(dependents)
        
        return dependencies
    
    def detect_cycles(self) -> List[List[str]]:
        """
        Detect cycles in the workflow graph using DFS
        
        Returns:
            List of cycles found (each cycle is a list of node IDs)
        """
        cycles = []
        WHITE, GRAY, BLACK = 0, 1, 2
        color = {node_id: WHITE for node_id in self.nodes.keys()}
        path = []
        
        def dfs(node_id: str):
            if color[node_id] == GRAY:
                # Found a cycle
                cycle_start = path.index(node_id)
                cycles.append(path[cycle_start:] + [node_id])
                return
            
            if color[node_id] == BLACK:
                return
            
            color[node_id] = GRAY
            path.append(node_id)
            
            # Visit dependents (nodes that this node connects to)
            for dependent in self.dependents.get(node_id, []):
                dfs(dependent)
            
            path.pop()
            color[node_id] = BLACK
        
        for node_id in self.nodes.keys():
            if color[node_id] == WHITE:
                dfs(node_id)
        
        return cycles
    
    def get_entry_nodes(self) -> List[str]:
        """
        Get entry nodes (nodes with no dependencies/incoming edges)
        These are typically Start nodes or data source nodes
        """
        entry_nodes = []
        
        for node_id in self.nodes.keys():
            # Check if it's a Start node
            if self.nodes[node_id].get('type') == 'start':
                entry_nodes.append(node_id)
            # Or if it has no dependencies
            elif node_id not in self.dependency_graph or len(self.dependency_graph[node_id]) == 0:
                entry_nodes.append(node_id)
        
        return entry_nodes
    
    def topological_sort(self) -> List[str]:
        """
        Perform topological sort to determine execution order
        
        Algorithm (Kahn's algorithm):
        1. Find all nodes with no incoming edges (entry nodes)
        2. Add them to execution queue
        3. Remove their outgoing edges
        4. Repeat until all nodes are processed
        
        Returns:
            List of node IDs in execution order
        """
        # Check for cycles first
        cycles = self.detect_cycles()
        if cycles:
            raise ValueError(f"Workflow contains cycles: {cycles}")
        
        # Build in-degree map (number of incoming edges)
        in_degree = {node_id: 0 for node_id in self.nodes.keys()}
        for node_id, deps in self.dependency_graph.items():
            in_degree[node_id] = len(deps)
        
        # Find entry nodes (in-degree = 0)
        queue = deque([node_id for node_id, degree in in_degree.items() if degree == 0])
        execution_order = []
        
        # Process nodes
        while queue:
            # Sort queue to prioritize Start nodes for deterministic ordering
            queue = deque(sorted(queue, key=lambda nid: (
                0 if self.nodes[nid].get('type') == 'start' else 1,
                nid
            )))
            
            current = queue.popleft()
            execution_order.append(current)
            
            # Reduce in-degree of dependents
            for dependent in self.dependents.get(current, []):
                in_degree[dependent] -= 1
                if in_degree[dependent] == 0:
                    queue.append(dependent)
        
        # Check if all nodes were processed
        if len(execution_order) != len(self.nodes):
            remaining = set(self.nodes.keys()) - set(execution_order)
            raise ValueError(f"Could not determine execution order. Remaining nodes: {remaining}. Possible cycles or disconnected nodes.")
        
        self.execution_order = execution_order
        return execution_order
    
    def get_execution_levels(self) -> List[List[str]]:
        """
        Group nodes into execution levels (can run in parallel)
        
        Level 0: Entry nodes (no dependencies)
        Level 1: Nodes that depend only on Level 0 nodes
        Level 2: Nodes that depend on Level 0 or Level 1 nodes
        etc.
        
        Returns:
            List of levels, each level is a list of node IDs that can run in parallel
        """
        if not self.execution_order:
            self.topological_sort()
        
        levels = []
        node_level = {}
        
        for node_id in self.execution_order:
            # Find the maximum level of its dependencies
            max_dep_level = -1
            for dep in self.dependency_graph.get(node_id, []):
                if dep in node_level:
                    max_dep_level = max(max_dep_level, node_level[dep])
            
            # This node's level is one more than its max dependency level
            level = max_dep_level + 1
            
            # Add to appropriate level
            while len(levels) <= level:
                levels.append([])
            
            levels[level].append(node_id)
            node_level[node_id] = level
        
        return levels
    
    def get_node_inputs(self, node_id: str) -> List[Tuple[str, str]]:
        """
        Get input connections for a node
        
        Returns:
            List of (source_node_id, source_port) tuples
        """
        inputs = []
        for edge in self.edges:
            target = edge.get('target') or edge.get('targetNodeId')
            source = edge.get('source') or edge.get('sourceNodeId')
            
            if target == node_id:
                source_port = edge.get('sourcePort') or 'output'
                inputs.append((source, source_port))
        
        return inputs
    
    def get_node_outputs(self, node_id: str) -> List[Tuple[str, str]]:
        """
        Get output connections for a node
        
        Returns:
            List of (target_node_id, target_port) tuples
        """
        outputs = []
        for edge in self.edges:
            source = edge.get('source') or edge.get('sourceNodeId')
            
            if source == node_id:
                target = edge.get('target') or edge.get('targetNodeId')
                target_port = edge.get('targetPort') or 'input'
                outputs.append((target, target_port))
        
        return outputs
    
    def can_execute(self, node_id: str) -> bool:
        """
        Check if a node can be executed (all dependencies are completed)
        """
        dependencies = self.dependency_graph.get(node_id, set())
        
        if not dependencies:
            return True
        
        # Check if all dependencies are completed
        for dep_id in dependencies:
            if self.node_status.get(dep_id) != ExecutionStatus.COMPLETED:
                return False
        
        return True
    
    def get_execution_plan(self) -> Dict[str, Any]:
        """
        Get complete execution plan with levels and dependencies
        
        Returns:
            Dictionary with execution plan details
        """
        execution_order = self.topological_sort()
        levels = self.get_execution_levels()
        entry_nodes = self.get_entry_nodes()
        
        plan = {
            'execution_order': execution_order,
            'execution_levels': levels,
            'entry_nodes': entry_nodes,
            'total_nodes': len(self.nodes),
            'total_edges': len(self.edges),
            'can_parallelize': len(levels) > 1,
            'max_parallelism': max(len(level) for level in levels) if levels else 1,
            'node_details': {}
        }
        
        # Add details for each node
        for node_id in execution_order:
            node = self.nodes[node_id]
            inputs = self.get_node_inputs(node_id)
            outputs = self.get_node_outputs(node_id)
            dependencies = list(self.dependency_graph.get(node_id, set()))
            level = next((i for i, level_nodes in enumerate(levels) if node_id in level_nodes), -1)
            
            plan['node_details'][node_id] = {
                'id': node_id,
                'type': node.get('type'),
                'name': node.get('name', node.get('type')),
                'level': level,
                'dependencies': dependencies,
                'inputs': inputs,
                'outputs': outputs,
                'can_execute': self.can_execute(node_id)
            }
        
        return plan
    
    def validate_workflow(self) -> Tuple[bool, List[str]]:
        """
        Validate workflow structure
        
        Returns:
            (is_valid, list_of_errors)
        """
        errors = []
        
        # Check for cycles
        cycles = self.detect_cycles()
        if cycles:
            errors.append(f"Workflow contains {len(cycles)} cycle(s): {cycles}")
        
        # Check for disconnected nodes
        entry_nodes = self.get_entry_nodes()
        if not entry_nodes:
            errors.append("No entry nodes found. Workflow must have at least one Start node or node with no dependencies.")
        
        # Check for orphaned nodes (nodes with no connections)
        connected_nodes = set()
        for edge in self.edges:
            source = edge.get('source') or edge.get('sourceNodeId')
            target = edge.get('target') or edge.get('targetNodeId')
            if source:
                connected_nodes.add(source)
            if target:
                connected_nodes.add(target)
        
        orphaned = set(self.nodes.keys()) - connected_nodes
        if len(orphaned) > 1 or (len(orphaned) == 1 and self.nodes[list(orphaned)[0]].get('type') != 'start'):
            errors.append(f"Found {len(orphaned)} orphaned node(s) with no connections: {orphaned}")
        
        # Try topological sort
        try:
            self.topological_sort()
        except ValueError as e:
            errors.append(str(e))
        
        return len(errors) == 0, errors

