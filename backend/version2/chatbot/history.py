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
    return bool(_parse_intents(intents_json))


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


def _parse_intents(intents_json: str | None) -> list[Intent]:
    if not intents_json:
        return []

    try:
        raw_intents = json.loads(intents_json)
    except json.JSONDecodeError:
        return []

    if isinstance(raw_intents, dict):
        raw_intents = [raw_intents]
    if not isinstance(raw_intents, list):
        return []

    intents: list[Intent] = []
    for intent in raw_intents:
        if not isinstance(intent, dict):
            continue
        intent_type = intent.get("type")
        if intent_type not in _VALID_INTENT_TYPES:
            continue
        try:
            confidence = float(intent.get("confidence", 0.0))
        except (TypeError, ValueError):
            confidence = 0.0
        intents.append(Intent(IntentType(intent_type), confidence))

    return intents


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
        SELECT intents, entities, has_intents, has_entities, is_complete
        FROM   messages
        WHERE  conversation_id = ?
          AND  role = 'user'
        ORDER BY created_at DESC, id DESC
        """,
        (conversation_id,),
    )
    for row in rows:
        intents_json = row["intents"]
        has_intent = row["has_intents"]
        has_entities = row["has_entities"]
        is_complete = row["is_complete"]
        

        # Gặp completed turn → dừng tìm kiếm
        if is_complete:
            return []

        # Tìm thấy message có intent nhưng chưa đủ entity
        if has_intent and not has_entities:
            return _parse_intents(intents_json)

    return []

def find_entities_in_history(
    conversation_id: str,
    include_completed: bool = True,
) -> list[MatchResult]:
    """
    Find the latest matched entity in the conversation history.

    The current user message is inserted before the chatbot runs, so the newest
    row often has no NLU data yet. Rows without matched entities are skipped.
    include_completed=True allows follow-up questions to reuse the entity from
    the latest completed turn.
    """
    rows = fetch_all(
        """
        SELECT entities, is_complete
        FROM   messages
        WHERE  conversation_id = ?
          AND  role = 'user'
        ORDER BY created_at DESC, id DESC
        """,
        (conversation_id,),
    )

    for row in rows:
        is_complete = row["is_complete"]
        matched_entities = _parse_matched_entities(row["entities"])

        if matched_entities:
            if include_completed or not is_complete:
                return matched_entities
            return []

        # A completed turn without a matched entity, such as general chat,
        # closes the previous topic and prevents stale entity carry-over.
        if is_complete:
            return []

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
