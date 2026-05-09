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

    # def get_user_info(self, conversation_id: str) -> Optional[Dict]:
    #     """
    #     Lấy thông tin user từ bảng conversations
    #     """
    #     query = """
    #     SELECT user_id, is_guest, metadata
    #     FROM conversations
    #     WHERE id = ?
    #     """
    #     row = fetch_one(query, (conversation_id,))

    #     if not row:
    #         return None

    #     return {
    #         "user_id": row["user_id"],
    #         "is_guest": row["is_guest"],
    #         "metadata": row["metadata"],
    #     }

    # def save_message(self, conversation_id: str, role: str, content: str) -> None:
    #     """
    #     Lưu message vào SQLite (cần bảng messages)
    #     """
    #     query = """
    #     INSERT INTO messages (conversation_id, role, content, timestamp)
    #     VALUES (?, ?, ?, CURRENT_TIMESTAMP)
    #     """
    #     execute_query(query, (conversation_id, role, content))

    
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
    # guest_c3a50890
    # conv_4cfb8c17
    print(get_conversation_history(conversation_id="conv_4cfb8c17", k=2))