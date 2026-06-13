from typing import Dict, List

from database.db import fetch_all

K_DEFAULT = 10


class ConversationMemoryManager:
    """Read recent conversation history from SQLite."""

    def get_history(self, conversation_id: str, k: int = K_DEFAULT) -> List[Dict]:
        return get_conversation_history(conversation_id, k)


def get_conversation_history(conversation_id: str, k: int = K_DEFAULT) -> List[Dict]:
    query = """
    SELECT role, content, created_at
    FROM messages
    WHERE conversation_id = ?
    ORDER BY created_at DESC
    LIMIT ?
    """

    rows = fetch_all(query, (conversation_id, k))

    return [
        {
            "role": row["role"],
            "content": row["content"],
            "created_at": row["created_at"],
        }
        for row in reversed(rows)
    ]
