"""
Industry Scenarios Service - Manage scenarios (use cases, dataset ideas, competition ideas) per industry
Compatible with Community platform for consistency
"""
from typing import List, Dict, Optional, Tuple
from app import db
from app.models.industry_scenario_definition import IndustryScenarioDefinition
import logging

logger = logging.getLogger(__name__)


class IndustryScenariosService:
    """Service for managing industry scenarios"""
    
    @staticmethod
    def get_scenarios_for_industry(industry: str, scenario_type: Optional[str] = None) -> List[Dict]:
        """Get all scenarios for an industry, optionally filtered by type"""
        query = IndustryScenarioDefinition.query.filter_by(industry=industry, is_active=True)
        
        if scenario_type:
            query = query.filter_by(scenario_type=scenario_type)
        
        scenarios = query.order_by(IndustryScenarioDefinition.priority.desc(), IndustryScenarioDefinition.created_at.desc()).all()
        return [scenario.to_dict() for scenario in scenarios]
    
    @staticmethod
    def get_use_cases(industry: str) -> List[Dict]:
        """Get use cases for an industry"""
        return IndustryScenariosService.get_scenarios_for_industry(industry, scenario_type='use_case')
    
    @staticmethod
    def get_dataset_ideas(industry: str) -> List[Dict]:
        """Get dataset ideas for an industry"""
        return IndustryScenariosService.get_scenarios_for_industry(industry, scenario_type='dataset_idea')
    
    @staticmethod
    def get_competition_ideas(industry: str) -> List[Dict]:
        """Get competition ideas for an industry"""
        return IndustryScenariosService.get_scenarios_for_industry(industry, scenario_type='competition_idea')
    
    @staticmethod
    def get_scenario(scenario_id: int) -> Optional[IndustryScenarioDefinition]:
        """Get a single scenario by ID"""
        return IndustryScenarioDefinition.query.get(scenario_id)
