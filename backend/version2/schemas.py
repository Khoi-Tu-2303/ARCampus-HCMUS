from __future__ import annotations

from dataclasses import dataclass, field, asdict
from enum import Enum
from typing import List, Optional


class IntentType(Enum):
    NAVIGATION = "navigation"
    INFORM = "inform"
    GENERAL = "general"
    UNKNOWN = "unknown"


@dataclass
class Intent:
    type: IntentType
    confidence: float

@dataclass
class Entity:
    text: str
    label: str

@dataclass
class MatchResult:
    entity_text: str
    label : str
    matched_id: str
    score: float
    status: str  # "matched" | "unknown"
    def to_dict(self) -> dict:
        return asdict(self)


@dataclass
class NLUResult:
    intents: List[Intent]
    entities: List[Entity]
    raw_text: str


@dataclass
class ConversationTurn:
    user_text: str
    conversation_id : str
    intents : List[Intent]
    entities: List[MatchResult] = field(default_factory=list)
    
@dataclass
class ChatbotResponse:
    response: str
    intent: Intent
    entities: List[MatchResult] = field(default_factory=list)
    metadata: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "response": self.response,
            "intent": {
                "type": self.intent.type.value,
                "confidence": self.intent.confidence,
            },
            "entities": [e.to_dict() for e in self.entities],
            "metadata": self.metadata,
        }