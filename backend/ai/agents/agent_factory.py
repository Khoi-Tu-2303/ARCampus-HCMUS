from ai.agents.base_agent import BaseAgent
from ai.agents.agents import (
    FacilityAgent,
    AcademicAgent,
    ScheduleAgent,
    DocumentServiceAgent,
    NavigationAgent,
    GeneralAgent,
)

# Intent → Agent mapping
_INTENT_MAP : dict[str, str] = {
    "facility_query" : "facility_agent",
    "academic_query" : "academic_agent",
    "schedule_management" : "schedule_agent",
    "document_service" : "document_service_agent",
    "navigation" : "navigation_agent",
    "general_chat" : "general_agent"
}
_AGENT_MAP: dict[str, type[BaseAgent]] = {
    "facility_agent":  FacilityAgent,
    "academic_agent":  AcademicAgent,
    "schedule_agent":  ScheduleAgent,
    "document_service_agent":  DocumentServiceAgent,
    "navigation_agent": NavigationAgent,
    "general_agent": GeneralAgent,
}

# Cache instances (lazy singleton per agent type)
_agent_cache: dict[str, BaseAgent] = {}


class AgentFactory:
    """
    Trả về Agent instance phù hợp với intent.
    Mỗi Agent class chỉ được khởi tạo một lần (cached).
    """

    @staticmethod
    def get(intent: str) -> BaseAgent:
        agent_cls = _AGENT_MAP.get(_INTENT_MAP[intent], GeneralAgent)
        key = agent_cls.name

        if key not in _agent_cache:
            _agent_cache[key] = agent_cls()

        return _agent_cache[key]
