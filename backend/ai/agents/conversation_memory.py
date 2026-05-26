from typing import Any, List, Dict
from database.db import fetch_all

K_DEFAULT = 10


class ConversationMemoryManager:
    """
    Quản lý lịch sử hội thoại sử dụng SQLite
    """

    def get_history(self, conversation_id: str, k: int = K_DEFAULT) -> List[Dict]:
        """
        Lấy k tin nhắn gần nhất của conversation (cũ → mới)
        """
        return get_conversation_history(conversation_id, k)

    
def get_conversation_history(conversation_id: str, k: int = K_DEFAULT) -> List[Dict]:
    """
    Lấy k tin nhắn gần nhất từ SQLite (cũ → mới)
    """

    query = """
    SELECT role, content, created_at
    FROM messages
    WHERE conversation_id = ?
    ORDER BY created_at DESC
    LIMIT ?
    """

    rows = fetch_all(query, (conversation_id, k))

    # đảo ngược để cũ → mới
    return [
        {
            "role": row["role"],
            "content": row["content"],
            "created_at": row["created_at"],
        }
        for row in reversed(rows)
    ]
    
    
if __name__ == "__main__":
    print(get_conversation_history(conversation_id="conv_4cfb8c17", k=2))