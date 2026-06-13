import json
import logging
import uuid

from app.utils.chat_ai import get_answer
from database.db import execute_query, fetch_all, fetch_one, get_connection

logger = logging.getLogger(__name__)

DEFAULT_CONVERSATION_TITLE = "New Conversation"
DEFAULT_ASSISTANT_MESSAGE = (
    "Xin chào! Tôi là AR Campus Assistant, trợ lý thông minh hỗ trợ bạn "
    "khám phá khuôn viên HCMUS. Tôi có thể giúp bạn tìm kiếm phòng học, "
    "tòa nhà, địa điểm trong trường và cung cấp thông tin cần thiết một "
    "cách nhanh chóng."
)


def _is_guest_id(identifier) -> bool:
    return isinstance(identifier, str) and identifier.startswith("guest_")


def _is_user_id(identifier) -> bool:
    return isinstance(identifier, int) or (
        isinstance(identifier, str) and identifier.isdigit()
    )


def _conversation_exists(conversation_id: str):
    return fetch_one("SELECT id FROM conversations WHERE id = ?", (conversation_id,))


def create_conversation(owner_id):
    try:
        conv_id = f"conv_{uuid.uuid4().hex[:8]}"
        if _is_guest_id(str(owner_id)):
            execute_query(
                """
                INSERT INTO conversations (id, user_id, is_guest, conversation_title, metadata)
                VALUES (?, ?, ?, ?, ?)
                """,
                (
                    conv_id,
                    None,
                    1,
                    DEFAULT_CONVERSATION_TITLE,
                    json.dumps({"guest_id": str(owner_id)}, separators=(",", ":")),
                ),
            )
        elif _is_user_id(owner_id):
            execute_query(
                """
                INSERT INTO conversations (id, user_id, is_guest, conversation_title, metadata)
                VALUES (?, ?, ?, ?, ?)
                """,
                (conv_id, int(owner_id), 0, DEFAULT_CONVERSATION_TITLE, "{}"),
            )
        else:
            return None, (400, {"error": "Invalid user"})

        execute_query(
            """
            INSERT INTO messages (conversation_id, role, content, metadata)
            VALUES (?, ?, ?, ?)
            """,
            (conv_id, "assistant", DEFAULT_ASSISTANT_MESSAGE, "{}"),
        )

        return {"id_conversation": conv_id, "title": DEFAULT_CONVERSATION_TITLE}, None
    except Exception:
        return None, (400, {"error": "Invalid user"})


def get_conversations(owner_id):
    try:
        if _is_guest_id(str(owner_id)):
            rows = fetch_all(
                """
                SELECT id, conversation_title, created_at
                FROM conversations
                WHERE metadata LIKE ?
                ORDER BY created_at DESC, id DESC
                """,
                (f'%"guest_id":"{str(owner_id)}"%',),
            )
        else:
            rows = fetch_all(
                """
                SELECT id, conversation_title, created_at
                FROM conversations
                WHERE user_id = ?
                ORDER BY created_at DESC, id DESC
                """,
                (int(owner_id),),
            )
        return {
            "conversations": [
                {
                    "id_conversation": row["id"],
                    "title": row["conversation_title"],
                    "created_at": row["created_at"],
                }
                for row in rows
            ]
        }, None
    except Exception:
        return {"conversations": []}, None


def get_messages(conversation_id):
    if not _conversation_exists(conversation_id):
        return None, (404, {"error": "Conversation not found"})

    rows = fetch_all(
        """
        SELECT id, conversation_id, role, content, created_at, metadata
        FROM messages
        WHERE conversation_id = ?
        ORDER BY created_at ASC, id ASC
        """,
        (conversation_id,),
    )

    return {
        "messages": [
            {
                "id": str(row["id"]),
                "conversation_id": row["conversation_id"],
                "role": row["role"],
                "content": row["content"],
                "created_at": row["created_at"],
                "metadata": json.loads(row["metadata"]),
            }
            for row in rows
        ]
    }, None


def send_message(conversation_id: str, content: str):
    if not _conversation_exists(conversation_id):
        return None, (404, {"error": "Conversation not found"})

    try:
        conn = get_connection()
        try:
            cursor = conn.cursor()

            cursor.execute(
                """
                INSERT INTO messages (conversation_id, role, content)
                VALUES (?, ?, ?)
                """,
                (conversation_id, "user", content),
            )
            user_id = cursor.lastrowid

            answer = get_answer(conversation_id, content)
            assistant_content = answer["response"]
            intent = answer["intent"]
            entities = answer["entities"]
            metadata = answer["metadata"]

            cursor.execute(
                """
                UPDATE messages
                SET intents = ?,
                    entities = ?
                WHERE id = ?
                """,
                (
                    json.dumps(intent, ensure_ascii=False),
                    json.dumps(entities, ensure_ascii=False),
                    user_id,
                ),
            )

            cursor.execute(
                """
                INSERT INTO messages (conversation_id, role, content, metadata)
                VALUES (?, ?, ?, ?)
                """,
                (
                    conversation_id,
                    "assistant",
                    assistant_content,
                    json.dumps(metadata, ensure_ascii=False),
                ),
            )
            assistant_id = cursor.lastrowid

            conn.commit()

            user_row = cursor.execute(
                """
                SELECT created_at, metadata
                FROM messages
                WHERE id = ?
                """,
                (user_id,),
            ).fetchone()

            assistant_row = cursor.execute(
                """
                SELECT created_at
                FROM messages
                WHERE id = ?
                """,
                (assistant_id,),
            ).fetchone()

        finally:
            conn.close()

        return {
            "user_message": {
                "id": str(user_id),
                "conversation_id": conversation_id,
                "role": "user",
                "content": content,
                "created_at": user_row["created_at"],
                "metadata": user_row["metadata"],
            },
            "assistant_message": {
                "id": str(assistant_id),
                "conversation_id": conversation_id,
                "role": "assistant",
                "content": assistant_content,
                "created_at": assistant_row["created_at"],
                "metadata": metadata,
            },
        }, None
    except Exception:
        logger.exception("Failed to send message for conversation %s", conversation_id)
        return None, (500, {"error": "Internal server error"})


def rename_conversation(conversation_id: str, title: str):
    if not _conversation_exists(conversation_id):
        return None, (404, {"error": "Conversation not found"})

    execute_query(
        "UPDATE conversations SET conversation_title = ? WHERE id = ?",
        (title, conversation_id),
    )
    return {"message": "Conversation updated"}, None


def delete_conversation(conversation_id: str):
    if not _conversation_exists(conversation_id):
        return None, (404, {"error": "Conversation not found"})

    execute_query("DELETE FROM conversations WHERE id = ?", (conversation_id,))
    return {"message": "Conversation deleted"}, None


def delete_guest_data(guest_id: str):
    rows = fetch_all(
        "SELECT id FROM conversations WHERE metadata LIKE ?",
        (f'%"guest_id":"{guest_id}"%',),
    )
    if not rows:
        return None, (404, {"error": "Guest not found"})

    execute_query(
        "DELETE FROM conversations WHERE metadata LIKE ?",
        (f'%"guest_id":"{guest_id}"%',),
    )
    return {"status": "success"}, None
