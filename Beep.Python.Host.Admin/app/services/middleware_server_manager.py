"""
Middleware Server Manager

Orchestrator that controls, filters, forwards, and schedules all services.
Provides a clear API interface for developers to configure routing rules,
access control, and service integration.
"""
import logging
import re
import json
from typing import Dict, List, Optional, Any, Callable, Tuple
from dataclasses import dataclass, field, asdict
from datetime import datetime
from enum import Enum
from flask import has_app_context, request

from app.services.ai_service_orchestrator import get_orchestrator, ServiceType
from app.services.middleware_config import MiddlewareConfig
from app.services.rag_service import RAGService
from app.services.api_token_manager import get_api_token_manager

logger = logging.getLogger(__name__)


class RuleType(Enum):
    """Types of routing rules"""
    KEYWORD = "keyword"  # Match keywords in request
    PATTERN = "pattern"  # Match regex pattern
    QUESTION = "question"  # Match specific questions
    USER = "user"  # Match user ID/role
    CONTEXT = "context"  # Match context/domain


class ActionType(Enum):
    """Types of actions to take"""
    ROUTE = "route"  # Route to specific service/API
    BLOCK = "block"  # Block the request
    FILTER = "filter"  # Filter/modify the request
    SCHEDULE = "schedule"  # Schedule for later execution
    CHAIN = "chain"  # Chain multiple services


@dataclass
class RoutingRule:
    """A routing rule for request handling"""
    id: str
    name: str
    description: str
    rule_type: RuleType
    pattern: str  # Keyword, regex, or question pattern
    action: ActionType
    target_service: Optional[str] = None  # Service to route to
    target_api: Optional[str] = None  # Specific API endpoint
    priority: int = 0  # Higher priority rules execute first
    enabled: bool = True
    conditions: Dict[str, Any] = field(default_factory=dict)  # Additional conditions
    created_at: str = field(default_factory=lambda: datetime.now().isoformat())
    updated_at: str = field(default_factory=lambda: datetime.now().isoformat())
    
    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'rule_type': self.rule_type.value,
            'pattern': self.pattern,
            'action': self.action.value,
            'target_service': self.target_service,
            'target_api': self.target_api,
            'priority': self.priority,
            'enabled': self.enabled,
            'conditions': self.conditions,
            'created_at': self.created_at,
            'updated_at': self.updated_at
        }
    
    @classmethod
    def from_dict(cls, data: dict) -> 'RoutingRule':
        return cls(
            id=data['id'],
            name=data['name'],
            description=data['description'],
            rule_type=RuleType(data['rule_type']),
            pattern=data['pattern'],
            action=ActionType(data['action']),
            target_service=data.get('target_service'),
            target_api=data.get('target_api'),
            priority=data.get('priority', 0),
            enabled=data.get('enabled', True),
            conditions=data.get('conditions', {}),
            created_at=data.get('created_at', datetime.now().isoformat()),
            updated_at=data.get('updated_at', datetime.now().isoformat())
        )


@dataclass
class AccessPolicy:
    """Access control policy"""
    id: str
    name: str
    description: str
    service: str  # Service name (e.g., 'rag', 'llm')
    resource: Optional[str] = None  # Specific resource (e.g., collection_id)
    user_id: Optional[str] = None  # Specific user
    user_role: Optional[str] = None  # User role
    allowed: bool = True  # True = allow, False = deny
    conditions: Dict[str, Any] = field(default_factory=dict)
    created_at: str = field(default_factory=lambda: datetime.now().isoformat())
    updated_at: str = field(default_factory=lambda: datetime.now().isoformat())
    
    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'service': self.service,
            'resource': self.resource,
            'user_id': self.user_id,
            'user_role': self.user_role,
            'allowed': self.allowed,
            'conditions': self.conditions,
            'created_at': self.created_at,
            'updated_at': self.updated_at
        }
    
    @classmethod
    def from_dict(cls, data: dict) -> 'AccessPolicy':
        return cls(
            id=data['id'],
            name=data['name'],
            description=data['description'],
            service=data['service'],
            resource=data.get('resource'),
            user_id=data.get('user_id'),
            user_role=data.get('user_role'),
            allowed=data.get('allowed', True),
            conditions=data.get('conditions', {}),
            created_at=data.get('created_at', datetime.now().isoformat()),
            updated_at=data.get('updated_at', datetime.now().isoformat())
        )


class MiddlewareServerManager:
    """
    Main middleware server manager that orchestrates all services.
    
    Features:
    - Request routing based on keywords/patterns
    - Access control for services
    - Request filtering and modification
    - Service chaining and scheduling
    - Developer-friendly API
    """
    
    def __init__(self):
        self._rules: List[RoutingRule] = []
        self._policies: List[AccessPolicy] = []
        self._orchestrator = get_orchestrator()
        try:
            self._rag_service = RAGService()
        except:
            self._rag_service = None
        self._load_rules()
        self._load_policies()
    
    def _load_rules(self):
        """Load routing rules from database"""
        if not has_app_context():
            return
        
        try:
            # Try to load from new database tables first
            try:
                from app.models.middleware import RoutingRule as DBRoutingRule
                db_rules = DBRoutingRule.query.filter_by(enabled=True).all()
                if db_rules:
                    # Convert DB models to dataclass instances
                    self._rules = [RoutingRule.from_dict(r.to_dict()) for r in db_rules]
                    self._rules.sort(key=lambda r: r.priority, reverse=True)
                    logger.info(f"Loaded {len(self._rules)} routing rules from database")
                    return
            except Exception as e:
                logger.debug(f"Could not load from database tables (may not exist yet): {e}")
            
            # Fallback to Settings JSON (for migration compatibility)
            from app.models.core import Setting
            rules_json = Setting.get('middleware_routing_rules', '[]')
            rules_data = json.loads(rules_json) if rules_json else []
            self._rules = [RoutingRule.from_dict(r) for r in rules_data]
            # Sort by priority (higher first)
            self._rules.sort(key=lambda r: r.priority, reverse=True)
        except Exception as e:
            logger.error(f"Error loading routing rules: {e}")
            self._rules = []
    
    def _save_rules(self):
        """Save routing rules to database"""
        if not has_app_context():
            return
        
        try:
            # Try to save to new database tables first
            try:
                from app.models.middleware import RoutingRule as DBRoutingRule
                from app.database import db
                
                # Get all existing rules
                existing_ids = {r.id for r in self._rules}
                
                # Delete rules that no longer exist
                DBRoutingRule.query.filter(~DBRoutingRule.id.in_(existing_ids)).delete()
                
                # Update or create rules
                for rule in self._rules:
                    db_rule = DBRoutingRule.query.get(rule.id)
                    if db_rule:
                        # Update existing
                        db_rule.name = rule.name
                        db_rule.description = rule.description
                        db_rule.rule_type = rule.rule_type
                        db_rule.pattern = rule.pattern
                        db_rule.action = rule.action
                        db_rule.target_service = rule.target_service
                        db_rule.target_api = rule.target_api
                        db_rule.priority = rule.priority
                        db_rule.enabled = rule.enabled
                        db_rule.set_conditions(rule.conditions)
                        db_rule.updated_at = datetime.now()
                    else:
                        # Create new
                        db_rule = DBRoutingRule(
                            id=rule.id,
                            name=rule.name,
                            description=rule.description,
                            rule_type=rule.rule_type,
                            pattern=rule.pattern,
                            action=rule.action,
                            target_service=rule.target_service,
                            target_api=rule.target_api,
                            priority=rule.priority,
                            enabled=rule.enabled
                        )
                        db_rule.set_conditions(rule.conditions)
                        db.session.add(db_rule)
                
                db.session.commit()
                logger.info(f"Saved {len(self._rules)} routing rules to database")
                return
            except Exception as e:
                logger.debug(f"Could not save to database tables (may not exist yet): {e}")
                db.session.rollback()
            
            # Fallback to Settings JSON (for migration compatibility)
            from app.models.core import Setting
            rules_data = [r.to_dict() for r in self._rules]
            Setting.set('middleware_routing_rules', json.dumps(rules_data),
                       'Middleware routing rules configuration')
        except Exception as e:
            logger.error(f"Error saving routing rules: {e}")
    
    def _load_policies(self):
        """Load access policies from database"""
        if not has_app_context():
            return
        
        try:
            # Try to load from new database tables first
            try:
                from app.models.middleware import AccessPolicy as DBAccessPolicy
                db_policies = DBAccessPolicy.query.all()
                if db_policies:
                    # Convert DB models to dataclass instances
                    self._policies = [AccessPolicy.from_dict(p.to_dict()) for p in db_policies]
                    logger.info(f"Loaded {len(self._policies)} access policies from database")
                    return
            except Exception as e:
                logger.debug(f"Could not load from database tables (may not exist yet): {e}")
            
            # Fallback to Settings JSON (for migration compatibility)
            from app.models.core import Setting
            policies_json = Setting.get('middleware_access_policies', '[]')
            policies_data = json.loads(policies_json) if policies_json else []
            self._policies = [AccessPolicy.from_dict(p) for p in policies_data]
        except Exception as e:
            logger.error(f"Error loading access policies: {e}")
            self._policies = []
    
    def _save_policies(self):
        """Save access policies to database"""
        if not has_app_context():
            return
        
        try:
            # Try to save to new database tables first
            try:
                from app.models.middleware import AccessPolicy as DBAccessPolicy
                from app.database import db
                
                # Get all existing policies
                existing_ids = {p.id for p in self._policies}
                
                # Delete policies that no longer exist
                DBAccessPolicy.query.filter(~DBAccessPolicy.id.in_(existing_ids)).delete()
                
                # Update or create policies
                for policy in self._policies:
                    db_policy = DBAccessPolicy.query.get(policy.id)
                    if db_policy:
                        # Update existing
                        db_policy.name = policy.name
                        db_policy.description = policy.description
                        db_policy.service = policy.service
                        db_policy.resource = policy.resource
                        db_policy.user_id = policy.user_id
                        db_policy.user_role = policy.user_role
                        db_policy.allowed = policy.allowed
                        db_policy.set_conditions(policy.conditions)
                        db_policy.updated_at = datetime.now()
                    else:
                        # Create new
                        db_policy = DBAccessPolicy(
                            id=policy.id,
                            name=policy.name,
                            description=policy.description,
                            service=policy.service,
                            resource=policy.resource,
                            user_id=policy.user_id,
                            user_role=policy.user_role,
                            allowed=policy.allowed
                        )
                        db_policy.set_conditions(policy.conditions)
                        db.session.add(db_policy)
                
                db.session.commit()
                logger.info(f"Saved {len(self._policies)} access policies to database")
                return
            except Exception as e:
                logger.debug(f"Could not save to database tables (may not exist yet): {e}")
                db.session.rollback()
            
            # Fallback to Settings JSON (for migration compatibility)
            from app.models.core import Setting
            policies_data = [p.to_dict() for p in self._policies]
            Setting.set('middleware_access_policies', json.dumps(policies_data),
                       'Middleware access control policies')
        except Exception as e:
            logger.error(f"Error saving access policies: {e}")
    
    def add_rule(self, rule: RoutingRule) -> bool:
        """Add a new routing rule"""
        try:
            # Check if rule with same ID exists
            existing = next((r for r in self._rules if r.id == rule.id), None)
            if existing:
                # Update existing
                rule.updated_at = datetime.now().isoformat()
                index = self._rules.index(existing)
                self._rules[index] = rule
            else:
                rule.created_at = datetime.now().isoformat()
                rule.updated_at = datetime.now().isoformat()
                self._rules.append(rule)
            
            # Re-sort by priority
            self._rules.sort(key=lambda r: r.priority, reverse=True)
            self._save_rules()
            logger.info(f"Routing rule added/updated: {rule.name} (ID: {rule.id})")
            return True
        except Exception as e:
            logger.error(f"Error adding routing rule: {e}")
            return False
    
    def remove_rule(self, rule_id: str) -> bool:
        """Remove a routing rule"""
        try:
            self._rules = [r for r in self._rules if r.id != rule_id]
            self._save_rules()
            logger.info(f"Routing rule removed: {rule_id}")
            return True
        except Exception as e:
            logger.error(f"Error removing routing rule: {e}")
            return False
    
    def get_rules(self, enabled_only: bool = False) -> List[RoutingRule]:
        """Get all routing rules"""
        if enabled_only:
            return [r for r in self._rules if r.enabled]
        return self._rules.copy()
    
    def add_policy(self, policy: AccessPolicy) -> bool:
        """Add a new access policy"""
        try:
            existing = next((p for p in self._policies if p.id == policy.id), None)
            if existing:
                policy.updated_at = datetime.now().isoformat()
                index = self._policies.index(existing)
                self._policies[index] = policy
            else:
                policy.created_at = datetime.now().isoformat()
                policy.updated_at = datetime.now().isoformat()
                self._policies.append(policy)
            
            self._save_policies()
            logger.info(f"Access policy added/updated: {policy.name} (ID: {policy.id})")
            return True
        except Exception as e:
            logger.error(f"Error adding access policy: {e}")
            return False
    
    def remove_policy(self, policy_id: str) -> bool:
        """Remove an access policy"""
        try:
            self._policies = [p for p in self._policies if p.id != policy_id]
            self._save_policies()
            logger.info(f"Access policy removed: {policy_id}")
            return True
        except Exception as e:
            logger.error(f"Error removing access policy: {e}")
            return False
    
    def get_policies(self, service: Optional[str] = None) -> List[AccessPolicy]:
        """Get access policies, optionally filtered by service"""
        if service:
            return [p for p in self._policies if p.service == service]
        return self._policies.copy()
    
    def check_access(self, service: str, user_id: Optional[str] = None,
                    user_role: Optional[str] = None, resource: Optional[str] = None) -> Tuple[bool, Optional[str]]:
        """
        Check if user has access to a service/resource
        
        Returns:
            (allowed: bool, reason: Optional[str])
        """
        # Check policies for this service
        relevant_policies = [
            p for p in self._policies
            if p.service == service and (
                (p.user_id is None or p.user_id == user_id) and
                (p.user_role is None or p.user_role == user_role) and
                (p.resource is None or p.resource == resource)
            )
        ]
        
        if not relevant_policies:
            # No specific policy, default to allowed
            return True, None
        
        # Check policies (deny takes precedence)
        for policy in relevant_policies:
            if not policy.allowed:
                return False, f"Access denied by policy: {policy.name}"
        
        # If all policies allow, grant access
        return True, None
    
    def route_request(self, request_text: str, service_type: Optional[str] = None,
                     user_id: Optional[str] = None, user_role: Optional[str] = None,
                     context: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """
        Route a request based on rules
        
        Args:
            request_text: The request text (e.g., chat message, query)
            service_type: Intended service type (if known)
            user_id: User ID making the request
            user_role: User role
            context: Additional context
        
        Returns:
            Routing decision dict with:
            - action: What to do (route, block, filter, etc.)
            - target_service: Service to route to
            - target_api: Specific API endpoint
            - modified_request: Modified request (if filtered)
            - reason: Reason for the routing decision
        """
        # Check access first
        if service_type:
            allowed, reason = self.check_access(service_type, user_id, user_role)
            if not allowed:
                return {
                    'action': 'block',
                    'reason': reason,
                    'blocked': True
                }
        
        # Check routing rules
        for rule in self._rules:
            if not rule.enabled:
                continue
            
            # Match rule based on type
            matched = False
            
            if rule.rule_type == RuleType.KEYWORD:
                # Simple keyword matching (case-insensitive)
                keywords = [k.strip().lower() for k in rule.pattern.split(',')]
                request_lower = request_text.lower()
                matched = any(keyword in request_lower for keyword in keywords)
            
            elif rule.rule_type == RuleType.PATTERN:
                # Regex pattern matching
                try:
                    matched = bool(re.search(rule.pattern, request_text, re.IGNORECASE))
                except re.error:
                    logger.warning(f"Invalid regex pattern in rule {rule.id}: {rule.pattern}")
                    continue
            
            elif rule.rule_type == RuleType.QUESTION:
                # Question matching (exact or similar)
                question_lower = rule.pattern.lower()
                request_lower = request_text.lower()
                # Check if request contains the question or is similar
                matched = question_lower in request_lower or request_lower in question_lower
            
            elif rule.rule_type == RuleType.USER:
                # User-based matching
                if rule.pattern.startswith('role:'):
                    role = rule.pattern.split(':', 1)[1]
                    matched = user_role == role
                elif rule.pattern.startswith('user:'):
                    user_id_pattern = rule.pattern.split(':', 1)[1]
                    matched = user_id == user_id_pattern
                else:
                    # Direct user ID match
                    matched = user_id == rule.pattern
            
            elif rule.rule_type == RuleType.CONTEXT:
                # Context-based matching
                if context:
                    context_key = rule.pattern.split('.')[0] if '.' in rule.pattern else rule.pattern
                    matched = context_key in context
            
            if matched:
                # Rule matched - execute action
                result = {
                    'action': rule.action.value,
                    'rule_id': rule.id,
                    'rule_name': rule.name,
                    'reason': f"Matched rule: {rule.name}"
                }
                
                if rule.action == ActionType.ROUTE:
                    result['target_service'] = rule.target_service
                    result['target_api'] = rule.target_api
                
                elif rule.action == ActionType.BLOCK:
                    result['blocked'] = True
                    result['reason'] = rule.conditions.get('message', 'Request blocked by routing rule')
                
                elif rule.action == ActionType.FILTER:
                    # Apply filters from conditions
                    modified_request = request_text
                    filters = rule.conditions.get('filters', {})
                    
                    # Remove keywords
                    if 'remove_keywords' in filters:
                        for keyword in filters['remove_keywords']:
                            modified_request = modified_request.replace(keyword, '')
                    
                    # Replace patterns
                    if 'replace' in filters:
                        for old, new in filters['replace'].items():
                            modified_request = modified_request.replace(old, new)
                    
                    result['modified_request'] = modified_request.strip()
                
                return result
        
        # No rule matched - default routing
        return {
            'action': 'route',
            'target_service': service_type,
            'reason': 'No matching rule, using default routing'
        }
    
    def _extract_auth_from_request(self) -> Tuple[Optional[str], Optional[str], Optional[str]]:
        """
        Extract authentication info from request headers
        
        Returns:
            (user_id, user_role, token)
        """
        # Try to get from Authorization header (Bearer token)
        auth_header = request.headers.get('Authorization', '')
        token = None
        if auth_header.startswith('Bearer '):
            token = auth_header[7:].strip()
        
        # Try to get from X-API-Token header
        if not token:
            token = request.headers.get('X-API-Token')
        
        # Validate token if provided
        user_id = None
        user_role = None
        if token:
            token_mgr = get_api_token_manager()
            user_info = token_mgr.validate_token(token)
            if user_info:
                user_id = user_info.get('user_id')
                user_role = user_info.get('user_role')
        
        # Fallback to headers if no token
        if not user_id:
            user_id = request.headers.get('X-User-ID')
            user_role = request.headers.get('X-User-Role')
        
        return user_id, user_role, token
    
    def process_request(self, request_text: str, service_type: str,
                       user_id: Optional[str] = None, user_role: Optional[str] = None,
                       **kwargs) -> Dict[str, Any]:
        """
        Process a request through the middleware
        
        This is the main entry point for all service requests.
        It handles routing, access control, filtering, and execution.
        
        If user_id/user_role are not provided, tries to extract from request headers
        (Authorization: Bearer token or X-API-Token header).
        """
        # Extract auth from request if not provided
        if not user_id and has_app_context():
            req_user_id, req_user_role, token = self._extract_auth_from_request()
            if req_user_id:
                user_id = req_user_id
            if req_user_role:
                user_role = req_user_role
        
        # Step 1: Route the request
        routing = self.route_request(
            request_text=request_text,
            service_type=service_type,
            user_id=user_id,
            user_role=user_role,
            context=kwargs.get('context')
        )
        
        # Step 2: Handle blocking
        if routing.get('blocked'):
            return {
                'success': False,
                'error': routing.get('reason', 'Request blocked'),
                'blocked': True
            }
        
        # Step 3: Get the actual request text (may be filtered)
        actual_request = routing.get('modified_request', request_text)
        
        # Step 4: Determine target service
        target_service = routing.get('target_service') or service_type
        
        # Step 5: Check access to target service
        allowed, reason = self.check_access(target_service, user_id, user_role)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied',
                'blocked': True
            }
        
        # Step 6: Execute the request
        try:
            # Special handling for RAG service - check access before querying
            if target_service == 'rag' and self._rag_service:
                # Check RAG-specific access
                collection_id = kwargs.get('collection_id')
                allowed, reason = self.check_access('rag', user_id, user_role, collection_id)
                if not allowed:
                    return {
                        'success': False,
                        'error': reason or 'Access denied to RAG collection',
                        'blocked': True
                    }
            
            service_enum = ServiceType(target_service)
            method = routing.get('target_api') or kwargs.get('method', 'default')
            
            # Prepare arguments
            args = kwargs.copy()
            if 'messages' in args:
                # Update messages with filtered request
                if isinstance(args['messages'], list) and args['messages']:
                    args['messages'][-1]['content'] = actual_request
            elif 'prompt' in args:
                args['prompt'] = actual_request
            elif 'query' in args:
                args['query'] = actual_request
            else:
                args['text'] = actual_request
            
            # Call service via orchestrator
            result = self._orchestrator.call_service(service_enum, method, **args)
            
            # Add middleware metadata
            result['middleware'] = {
                'routed': True,
                'rule_matched': routing.get('rule_id'),
                'original_request': request_text,
                'processed_request': actual_request
            }
            
            return result
            
        except ValueError:
            return {
                'success': False,
                'error': f'Invalid service type: {target_service}'
            }
        except Exception as e:
            logger.error(f"Error processing request: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def is_running(self) -> bool:
        """Check if middleware server is running"""
        return True  # Always running as part of the Flask app
    
    # =============================================================================
    # High-Level Service Methods - Direct Operations
    # =============================================================================
    
    def chat(self, messages: List[Dict], model_id: Optional[str] = None,
            user_id: Optional[str] = None, user_role: Optional[str] = None,
            **config) -> Dict[str, Any]:
        """
        Chat with LLM through middleware
        
        Args:
            messages: List of chat messages
            model_id: Model ID to use
            user_id: User ID
            user_role: User role
            **config: Additional LLM config (temperature, max_tokens, etc.)
        
        Returns:
            Chat response with middleware metadata
        """
        # Extract request text from last message
        request_text = messages[-1].get('content', '') if messages else ''
        
        return self.process_request(
            request_text=request_text,
            service_type='llm',
            user_id=user_id,
            user_role=user_role,
            method='chat',
            messages=messages,
            model_id=model_id,
            config=config
        )
    
    def rag_query(self, query: str, collection_id: Optional[str] = None,
                 user_id: Optional[str] = None, user_role: Optional[str] = None,
                 max_results: int = 5, **kwargs) -> Dict[str, Any]:
        """
        Query RAG through middleware
        
        Args:
            query: Search query
            collection_id: RAG collection ID
            user_id: User ID
            user_role: User role
            max_results: Maximum results to return
            **kwargs: Additional RAG parameters
        
        Returns:
            RAG query results with middleware metadata
        """
        # Check access first
        allowed, reason = self.check_access('rag', user_id, user_role, collection_id)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to RAG',
                'blocked': True
            }
        
        if not self._rag_service:
            return {
                'success': False,
                'error': 'RAG service not available'
            }
        
        try:
            result = self._rag_service.retrieve_context(
                query=query,
                user_id=user_id,
                collection_ids=[collection_id] if collection_id else None,
                max_results=max_results,
                **kwargs
            )
            
            return {
                'success': True,
                'contexts': result.get('contexts', []),
                'metadata': result.get('metadata', {}),
                'middleware': {
                    'routed': True,
                    'access_checked': True
                }
            }
        except Exception as e:
            logger.error(f"RAG query error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def rag_add_documents(self, documents: List[Dict[str, Any]], collection_id: str,
                         user_id: Optional[str] = None, user_role: Optional[str] = None) -> Dict[str, Any]:
        """
        Add documents to RAG collection through middleware
        
        Args:
            documents: List of documents with {content, source?, metadata?}
            collection_id: Target collection ID
            user_id: User ID
            user_role: User role
        
        Returns:
            Add result with middleware metadata
        """
        # Check access
        allowed, reason = self.check_access('rag', user_id, user_role, collection_id)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to RAG collection',
                'blocked': True
            }
        
        if not self._rag_service:
            return {
                'success': False,
                'error': 'RAG service not available'
            }
        
        try:
            result = self._rag_service.add_documents(
                documents=documents,
                collection_id=collection_id
            )
            
            return {
                'success': result.get('success', False),
                'added': result.get('added', 0),
                'message': result.get('message', ''),
                'middleware': {
                    'routed': True,
                    'access_checked': True
                }
            }
        except Exception as e:
            logger.error(f"RAG add documents error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def rag_update_document(self, document_id: str, collection_id: str,
                           content: Optional[str] = None, metadata: Optional[Dict] = None,
                           user_id: Optional[str] = None, user_role: Optional[str] = None) -> Dict[str, Any]:
        """
        Update a document in RAG collection through middleware
        
        Args:
            document_id: Document ID to update
            collection_id: Collection ID
            content: New content (optional)
            metadata: New metadata (optional)
            user_id: User ID
            user_role: User role
        
        Returns:
            Update result
        """
        # Check access
        allowed, reason = self.check_access('rag', user_id, user_role, collection_id)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to RAG collection',
                'blocked': True
            }
        
        if not self._rag_service:
            return {
                'success': False,
                'error': 'RAG service not available'
            }
        
        try:
            # RAG service might not have update_document, so we'll delete and re-add
            # Or use the provider's update method if available
            # For now, return not implemented - can be enhanced based on RAG provider capabilities
            return {
                'success': False,
                'error': 'Document update not yet implemented. Use delete + add instead.'
            }
        except Exception as e:
            logger.error(f"RAG update document error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def rag_remove_documents(self, document_ids: List[str], collection_id: str,
                            user_id: Optional[str] = None, user_role: Optional[str] = None) -> Dict[str, Any]:
        """
        Remove documents from RAG collection through middleware
        
        Args:
            document_ids: List of document IDs to remove
            collection_id: Collection ID
            user_id: User ID
            user_role: User role
        
        Returns:
            Remove result
        """
        # Check access
        allowed, reason = self.check_access('rag', user_id, user_role, collection_id)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to RAG collection',
                'blocked': True
            }
        
        if not self._rag_service:
            return {
                'success': False,
                'error': 'RAG service not available'
            }
        
        try:
            result = self._rag_service.delete_documents(
                document_ids=document_ids,
                collection_id=collection_id
            )
            
            return {
                'success': result.get('success', False),
                'deleted': result.get('deleted', 0),
                'message': result.get('message', ''),
                'middleware': {
                    'routed': True,
                    'access_checked': True
                }
            }
        except Exception as e:
            logger.error(f"RAG remove documents error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def extract_document(self, file_content: bytes, filename: str,
                        user_id: Optional[str] = None, user_role: Optional[str] = None) -> Dict[str, Any]:
        """
        Extract text from document through middleware
        
        Args:
            file_content: Document file content (bytes)
            filename: Filename
            user_id: User ID
            user_role: User role
        
        Returns:
            Extraction result
        """
        # Check access
        allowed, reason = self.check_access('document_extraction', user_id, user_role)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to document extraction',
                'blocked': True
            }
        
        try:
            from app.services.document_extractor import get_document_extractor
            extractor = get_document_extractor()
            
            result = extractor.extract(
                file_content=file_content,
                filename=filename
            )
            
            return {
                'success': result.success,
                'text': result.text,
                'metadata': result.metadata,
                'error': result.error,
                'middleware': {
                    'routed': True,
                    'access_checked': True
                }
            }
        except Exception as e:
            logger.error(f"Document extraction error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def create_job(self, job_name: str, job_type: str, schedule: str,
                  function_name: str, function_args: Dict[str, Any],
                  user_id: Optional[str] = None, user_role: Optional[str] = None) -> Dict[str, Any]:
        """
        Create a scheduled job through middleware
        
        Args:
            job_name: Job name
            job_type: Job type (e.g., 'llm', 'rag', 'extraction')
            schedule: Cron expression or interval
            function_name: Function to execute
            function_args: Arguments for the function
            user_id: User ID
            user_role: User role
        
        Returns:
            Job creation result
        """
        # Check access
        allowed, reason = self.check_access('job_scheduler', user_id, user_role)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to job scheduler',
                'blocked': True
            }
        
        try:
            from app.services.job_scheduler import get_job_scheduler
            from app.models.scheduled_jobs import ScheduledJob, JobModule, ScheduleType
            from app.database import db
            
            if not has_app_context():
                return {
                    'success': False,
                    'error': 'No Flask app context available'
                }
            
            scheduler = get_job_scheduler()
            
            # Determine module from job_type
            module_map = {
                'llm': JobModule.LLM.value,
                'rag': JobModule.RAG.value,
                'document_extraction': JobModule.DOCUMENT_EXTRACTION.value,
                'ml_models': JobModule.ML_MODELS.value,
                'system': JobModule.SYSTEM.value
            }
            module = module_map.get(job_type, JobModule.CUSTOM.value)
            
            # Parse schedule to determine type
            schedule_type = ScheduleType.CRON.value  # Default
            schedule_config = {}
            
            if ' ' in schedule:
                # Cron expression (e.g., "0 0 * * *")
                schedule_type = ScheduleType.CRON.value
                schedule_config = {'cron_expression': schedule}
            elif schedule.startswith('interval:'):
                # Interval format: "interval:3600" (seconds)
                schedule_type = ScheduleType.INTERVAL.value
                interval_seconds = int(schedule.split(':', 1)[1])
                schedule_config = {'interval_seconds': interval_seconds}
            elif schedule.startswith('once:'):
                # Once format: "once:2024-01-01T12:00:00"
                schedule_type = ScheduleType.ONCE.value
                run_date = schedule.split(':', 1)[1]
                schedule_config = {'run_date': run_date}
            else:
                # Try to parse as interval number (seconds)
                try:
                    interval_seconds = int(schedule)
                    schedule_type = ScheduleType.INTERVAL.value
                    schedule_config = {'interval_seconds': interval_seconds}
                except ValueError:
                    # Default to cron
                    schedule_config = {'cron_expression': schedule}
            
            # Create job in database
            job = ScheduledJob(
                name=job_name,
                module=module,
                job_type=job_type,
                schedule_type=schedule_type,
                schedule_config=schedule_config,
                function_name=function_name,
                parameters=function_args,
                is_active=True
            )
            db.session.add(job)
            db.session.commit()
            
            # Schedule the job
            scheduler.schedule_job(job.id)
            
            return {
                'success': True,
                'job_id': job.id,
                'message': 'Job created successfully',
                'middleware': {
                    'routed': True,
                    'access_checked': True
                }
            }
        except Exception as e:
            logger.error(f"Job creation error: {e}", exc_info=True)
            if has_app_context():
                from app.database import db
                db.session.rollback()
            return {
                'success': False,
                'error': str(e)
            }
    
    def check_job(self, job_id: str, user_id: Optional[str] = None,
                 user_role: Optional[str] = None) -> Dict[str, Any]:
        """
        Check job status through middleware
        
        Args:
            job_id: Job ID
            user_id: User ID
            user_role: User role
        
        Returns:
            Job status
        """
        # Check access
        allowed, reason = self.check_access('job_scheduler', user_id, user_role)
        if not allowed:
            return {
                'success': False,
                'error': reason or 'Access denied to job scheduler',
                'blocked': True
            }
        
        try:
            if not has_app_context():
                return {
                    'success': False,
                    'error': 'No Flask app context available'
                }
            
            from app.models.scheduled_jobs import ScheduledJob
            from app.database import db
            
            job = db.session.get(ScheduledJob, int(job_id))
            if not job:
                return {
                    'success': False,
                    'error': 'Job not found'
                }
            
            return {
                'success': True,
                'job': job.to_dict(),
                'middleware': {
                    'routed': True,
                    'access_checked': True
                }
            }
        except ValueError:
            return {
                'success': False,
                'error': f'Invalid job ID: {job_id}'
            }
        except Exception as e:
            logger.error(f"Job check error: {e}", exc_info=True)
            return {
                'success': False,
                'error': str(e)
            }
    
    def get_status(self) -> Dict[str, Any]:
        """Get middleware server status"""
        return {
            'running': self.is_running(),
            'rules_count': len(self._rules),
            'policies_count': len(self._policies),
            'enabled_rules': len([r for r in self._rules if r.enabled]),
            'services_available': self._orchestrator.list_available_services(),
            'services_status': self._orchestrator.get_service_status(),
            'capabilities': {
                'chat': True,
                'rag_query': True,
                'rag_add': True,
                'rag_update': False,  # Not yet implemented
                'rag_remove': True,
                'extract_document': True,
                'create_job': True,
                'check_job': True
            }
        }


# Singleton instance
_middleware_manager_instance = None


def get_middleware_server_manager() -> MiddlewareServerManager:
    """Get singleton instance of middleware server manager"""
    global _middleware_manager_instance
    if _middleware_manager_instance is None:
        _middleware_manager_instance = MiddlewareServerManager()
    return _middleware_manager_instance
