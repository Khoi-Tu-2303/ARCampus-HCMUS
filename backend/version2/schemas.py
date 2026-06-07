from __future__ import annotations

from dataclasses import dataclass, field
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


@dataclass
class NLUResult:
    intents: List[Intent]
    entities: List[Entity]
    raw_text: str


@dataclass
class ConversationTurn:
    user_text: str
    message_id : str
    intents : List[Intent]
    entities: List[MatchResult] = field(default_factory=list)