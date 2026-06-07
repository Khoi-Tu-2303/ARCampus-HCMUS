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

def handle_navigation(conversation_id : str,
    turn: ConversationTurn
) -> str:
    """
    Xử lý intent NAVIGATION.

    Args:
        turn:             Turn hiện tại (đã có entities sau entity linking).
        history_entities: Fallback entities lấy từ lịch sử nếu turn hiện tại rỗng.
    """
    matched = [e for e in turn.entities if e.status == "matched"]

    if not matched:
        history_entities = find_entities_in_history(conversation_id)
        if not history_entities:
            return _reply_ask_destination()
        matched = history_entities

    if len(matched) > 1:
        return _reply_ask_which_destination(matched)

    return _reply_navigate_to(matched[0])


# ------------------------------------------------------------------
# Inform
# ------------------------------------------------------------------

def handle_inform(conversation_id : str,
    turn: ConversationTurn
) -> str:
    """
    Xử lý intent INFORM.

    Ưu tiên entity của turn hiện tại.
    Nếu không có thì dùng entity trong lịch sử.
    """

    matched = [e for e in turn.entities if e.status == "matched"]

    if not matched:
        history_entities = find_entities_in_history(conversation_id)
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

def handle_unknown_or_general(conversation_id : str,
    turn: ConversationTurn
) -> str:
    """
    Xử lý trường hợp không có NAVIGATION lẫn INFORM.

    Args:
        turn:            Turn hiện tại.
        history_intent:  Intent gần nhất tìm được trong lịch sử (hoặc None).
        history_entities: Entities gần nhất tìm được trong lịch sử.
    """
    top_intent = _get_top_intent(turn)

    if top_intent.type == IntentType.UNKNOWN:
        
        # unknown + không có entities
        if not turn.entities:
            return _reply_not_support()

        history_intent = find_intent_in_history(conversation_id)
        # unknown + có entities nhưng không có intent history
        if len(history_intent) == 0:
            return _reply_ask_navigation_or_inform()

        # Có entity + biết intent từ history → dispatch lại
        
        if len(history_intent) > 1:
            return _reply_ask_navigation_or_inform()
        
        if history_intent[0].type.value == IntentType.NAVIGATION:
            return handle_navigation(conversation_id, turn)
        
        if history_intent[0].type.value == IntentType.INFORM:
            return handle_inform(conversation_id, turn)
        
        return _reply_not_support()

    if top_intent.type.value == IntentType.GENERAL:
        return _reply_general_chitchat()

    return _reply_not_support()


# ------------------------------------------------------------------
# Reply builders (private helpers)
# ------------------------------------------------------------------

def _reply_ask_enity_inform() -> str:
    return "Bạn cần thông tin về địa điểm nào?"

def _get_top_intent(turn: ConversationTurn)-> Intent | None:
    if not turn.intents:
        return None
    return max(turn.intents, key=lambda i: i.confidence)


def _reply_not_support() -> str:
    return "Xin lỗi, tôi chỉ hỗ trợ tìm kiếm thông tin hoặc chỉ đường đến các địa điểm trong trường."


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