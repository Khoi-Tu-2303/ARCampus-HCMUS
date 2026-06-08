"""
Handlers: mỗi hàm xử lý một nhánh nghiệp vụ và trả về response string.

Tách khỏi bot.py để:
- Dễ thêm nhánh mới mà không chạm vào orchestrator.
- Dễ mock / test từng nhánh độc lập.
- Mỗi handler nhận đúng dữ liệu cần thiết, không phụ thuộc vào state của Chatbot.
"""

from __future__ import annotations

from version2.schemas import ConversationTurn, IntentType, MatchResult, Intent
from version2.chatbot.history import find_entities_in_history, find_intent_in_history, update_message_nlu


# ------------------------------------------------------------------
# Navigation
# ------------------------------------------------------------------

def handle_navigation(turn: ConversationTurn) -> str:
    """
    Xử lý intent NAVIGATION.

    Args:
        turn:             Turn hiện tại (đã có entities sau entity linking).
        history_entities: Fallback entities lấy từ lịch sử nếu turn hiện tại rỗng.
    """
    unknown = [e for e in turn.entities if e.status == "unknown"]
    if len(unknown) > 0:
        return _handle_navigation_unknown(unknown)
    
    matched = [e for e in turn.entities if e.status == "matched"]

    if not matched:
        history_entities = find_entities_in_history(turn.conversation_id)
        if not history_entities:
            return _reply_ask_destination()
        matched = history_entities

    if len(matched) > 1:
        return _reply_ask_which_destination(matched)

    return _reply_navigate_to(matched[0])

def _handle_navigation_unknown(unknown: list[MatchResult]) -> str:
    names = [enity.entity_text for enity in unknown]
    return f"Hiện tại tôi không có thông tin về {",".join(names)}"


# ------------------------------------------------------------------
# Inform
# ------------------------------------------------------------------

def handle_inform(turn: ConversationTurn) -> str:
    """
    Xử lý intent INFORM.

    Ưu tiên entity của turn hiện tại.
    Nếu không có thì dùng entity trong lịch sử.
    """

    matched = [e for e in turn.entities if e.status == "matched"]
    if len(turn.entities) == 0:
        history_entities = find_entities_in_history(turn.conversation_id)
        if not history_entities:
            return _reply_ask_enity_inform()
        matched = history_entities

    keys = [
        e.matched_id
        for e in matched
        if e.matched_id
    ]

    return _reply_inform(keys)

# ------------------------------------------------------------------
# Unknown / General
# ------------------------------------------------------------------

def handle_unknown(turn: ConversationTurn) -> str:
    """
    Xử lý intent UNKNOWN.
    """

    # UNKNOWN + không có entity
    if not turn.entities:
        return _reply_not_support()

    history_intent = find_intent_in_history(turn.conversation_id)

    # Có entity nhưng không biết intent trước đó
    if len(history_intent) == 0:
        return _reply_ask_navigation_or_inform()

    last_intent = history_intent[0].type

    if last_intent == IntentType.NAVIGATION:
        return handle_navigation(turn)

    if last_intent == IntentType.INFORM:
        return handle_inform(turn)

    return _reply_not_support()

def handle_general(turn: ConversationTurn) -> str:
    """
    Xử lý intent GENERAL.
    """
    return _reply_general_chitchat(turn)

# ------------------------------------------------------------------
# Reply builders (private helpers)
# ------------------------------------------------------------------

def _reply_ask_enity_inform() -> str:
    return "Bạn cần thông tin về địa điểm nào?"


def _reply_not_support() -> str:
    return "Xin lỗi, tôi chỉ hỗ trợ tìm kiếm thông tin hoặc chỉ đường đến các địa điểm trong khuôn viên trường."


def _reply_ask_navigation_or_inform() -> str:
    return "Bạn cần hỗ trợ tìm đường hay hỏi thông tin về địa điểm?"


def _reply_ask_destination() -> str:
    return "Bạn hãy cho tôi biết rõ địa điểm bạn muốn đến!"


def _reply_ask_which_destination(entities: list[MatchResult]) -> str:
    names = ", ".join(e.entity_text for e in entities)
    return f"Bạn muốn đến địa điểm nào trong số: {names}?"


def _reply_navigate_to(entity: MatchResult) -> str:
    return f"Đang chỉ đường đến {entity.entity_text} (ID: {entity.matched_id})..."


def _reply_inform(keys: list[str]) -> str:
    return f"Thông tin về: {', '.join(keys)}..."


def _reply_general_chitchat() -> str:
    return "Xin chào! Tôi có thể giúp bạn tìm đường hoặc tra cứu thông tin địa điểm."