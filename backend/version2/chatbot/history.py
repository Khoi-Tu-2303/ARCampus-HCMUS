"""
Các hàm truy vấn lịch sử hội thoại — phiên bản dùng DB.

Logic find_intent_in_history:
  Tìm message gần nhất (role='user') thỏa mãn ĐỒNG THỜI:
    (A) Có intents (NAVIGATION hoặc INFORM)
    (B) Không có entities đã matched
    (C) Không tồn tại message nào SAU nó trong cùng conversation
        đã "hoàn thành" (có cả intents lẫn entities matched)
"""

from __future__ import annotations
import json
from dataclasses import dataclass

from database.db import fetch_all, execute_query
from version2.schemas import IntentType, MatchResult, Intent
from dataclasses import asdict


# ---------------------------------------------------------------------------
# Helpers nội bộ
# ---------------------------------------------------------------------------

_VALID_INTENT_TYPES = (IntentType.NAVIGATION.value, IntentType.INFORM.value)


def _has_valid_intent(intents_json: str | None) -> bool:
    """Kiểm tra JSON intents có chứa NAVIGATION hoặc INFORM không.
    [{type : str, confidence : float}]
    """
    if not intents_json:
        return False
    try:
        intents = json.loads(intents_json)
        return any(i.get("type") in _VALID_INTENT_TYPES for i in intents)
    except (json.JSONDecodeError, AttributeError):
        return False


def _has_matched_entities(entities_json: str | None) -> bool:
    """Kiểm tra JSON entities có chứa entity nào status='matched' không.
    [{entity_text : str, label : str, matched_id : str, score : float, status : str}]
    """
    if not entities_json:
        return False
    try:
        entities = json.loads(entities_json)
        return any(e.get("status") == "matched" for e in entities)
    except (json.JSONDecodeError, AttributeError):
        return False

def _parse_matched_entities(entities_json: str | None) -> list[MatchResult]:
    """Parse JSON entities, trả về list các entity đã matched."""
    if not entities_json:
        return []
    try:
        entities = json.loads(entities_json)
        return [
            MatchResult(**e)
            for e in entities
            if e.get("status") == "matched"
        ]
    except (json.JSONDecodeError, TypeError):
        return []


# ---------------------------------------------------------------------------
# API công khai
# ---------------------------------------------------------------------------

def find_intent_in_history(conversation_id: str) -> list[Intent]:
    """
    Tìm message gần nhất có intents hợp lệ nhưng chưa có entities matched,
    với điều kiện chưa có "completed turn" nào xảy ra sau message đó.

    "Completed turn" = message có CẢ intents hợp lệ VÀ entities matched
    → nghĩa là một truy vấn đã được giải quyết xong.

    Thuật toán:
      1. Lấy toàn bộ messages của conversation theo thứ tự giảm dần (mới → cũ).
      2. Duyệt từng message:
         - Nếu gặp "completed turn" → dừng ngay (mọi thứ trước đó không còn
           hiệu lực, user đang bắt đầu ý định mới).
         - Nếu gặp message có intent hợp lệ mà không có entity matched
           → đây là message cần tìm, trả về IntentType.
      3. Trả None nếu không tìm thấy.

    Trả về IntentType (NAVIGATION hoặc INFORM) hoặc None.
    """
    rows = fetch_all(
        """
        SELECT intents, entities
        FROM   messages
        WHERE  conversation_id = ?
          AND  role = 'user'
        ORDER BY created_at DESC
        """,
        (conversation_id,),
    )

    for row in rows:
        intents_json = row["intents"]
        entities_json = row["entities"]

        has_intent = _has_valid_intent(intents_json)
        has_entity = _has_matched_entities(entities_json)

        # Gặp completed turn → dừng tìm kiếm
        if has_intent and has_entity:
            return []

        # Tìm thấy message có intent nhưng chưa đủ entity
        if has_intent and not has_entity:
            intents = json.loads(intents_json)
            return [
                Intent(IntentType(intent.get("type")), float(intent.get("confidence")))
                for intent in intents
                if intent.get("type") in _VALID_INTENT_TYPES
            ]

    return []

def find_entities_in_history(conversation_id: str) -> list[MatchResult]:
    """
    Tìm message gần nhất có entities matched nhưng không có intent.

    Điều kiện:
      - Nếu phía sau nó đã có completed turn
        (intent hợp lệ + entity matched)
        => entities cũ không còn hiệu lực.
      - Chỉ lấy entities của message có entity nhưng không có intent.

    Trả về:
      list[MatchResult]
    """
    rows = fetch_all(
        """
        SELECT intents, entities
        FROM   messages
        WHERE  conversation_id = ?
          AND  role = 'user'
        ORDER BY created_at DESC
        """,
        (conversation_id,),
    )

    for row in rows:
        intents_json = row["intents"]
        entities_json = row["entities"]

        has_intent = _has_valid_intent(intents_json)
        has_entity = _has_matched_entities(entities_json)

        # completed turn -> stop
        if has_intent and has_entity:
            return []

        # entity nhưng không có intent
        if not has_intent and has_entity:
            return _parse_matched_entities(entities_json)

    return []

def update_message_nlu(
    message_id: int,
    intents: list[Intent],
    entities: list[MatchResult],
) -> None:
    """
    Cập nhật intents và entities cho message.
    """

    intents_json = json.dumps(
        [
            {"type": intent.type.value, "confidence" : intent.confidence}
            for intent in intents
        ],
        ensure_ascii=False,
    )

    entities_json = json.dumps(
        [
            asdict(entity)
            for entity in entities
        ],
        ensure_ascii=False,
    )
        
    execute_query(
        """
        UPDATE messages
        SET
            intents = ?,
            entities = ?
        WHERE id = ?
        """,
        (
            intents_json,
            entities_json,
            message_id,
        ),
    )
    
if __name__ == "__main__":
    pass