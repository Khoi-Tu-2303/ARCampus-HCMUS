import json
import uuid

from database.db import fetch_one, fetch_all, execute_query, get_connection
from app.utils.chat_ai import get_answer

def _is_guest_id(identifier) -> bool:
    return isinstance(identifier, str) and identifier.startswith('guest_')


def _is_user_id(identifier) -> bool:
    return isinstance(identifier, int) or (isinstance(identifier, str) and identifier.isdigit())


def _conversation_exists(conversation_id: str):
    return fetch_one('SELECT id FROM conversations WHERE id = ?', (conversation_id,))


def create_conversation(owner_id):
    try:
        conv_id = f'conv_{uuid.uuid4().hex[:8]}'
        if _is_guest_id(str(owner_id)):
            execute_query(
                'INSERT INTO conversations (id, user_id, is_guest, conversation_title, metadata) VALUES (?, ?, ?, ?, ?)',
                (conv_id, None, 1, 'New Conversation', json.dumps({'guest_id': str(owner_id)}, separators=(',', ':'))),
            )
        elif _is_user_id(owner_id):
            execute_query(
                'INSERT INTO conversations (id, user_id, is_guest, conversation_title, metadata) VALUES (?, ?, ?, ?, ?)',
                (conv_id, int(owner_id), 0, 'New Conversation', '{}'),
            )
        else:
            return None, (400, {'error': 'Invalid user'})
        return {'id': conv_id, 'title': 'New Conversation'}, None
    except Exception:
        return None, (400, {'error': 'Invalid user'})


def get_conversations(owner_id):
    try:
        if _is_guest_id(str(owner_id)):
            rows = fetch_all(
                'SELECT id, conversation_title, created_at FROM conversations WHERE metadata LIKE ? ORDER BY created_at DESC, id DESC',
                (f'%"guest_id":"{str(owner_id)}"%',),
            )
        else:
            rows = fetch_all(
                'SELECT id, conversation_title, created_at FROM conversations WHERE user_id = ? ORDER BY created_at DESC, id DESC',
                (int(owner_id),),
            )

        return [
            {'id': row['id'], 'title': row['conversation_title'], 'created_at': row['created_at']}
            for row in rows
        ], None
    except Exception:
        return [], None


def get_messages(conversation_id):
    if not _conversation_exists(conversation_id):
        return None, (404, {'error': 'Conversation not found'})

    rows = fetch_all(
        'SELECT id, role, content, created_at FROM messages WHERE conversation_id = ? ORDER BY created_at ASC, id ASC',
        (conversation_id,),
    )
    return [
        {'id': row['id'], 'role': row['role'], 'content': row['content'], 'created_at': row['created_at']}
        for row in rows
    ], None


def send_message(conversation_id: str, content: str):
    if not _conversation_exists(conversation_id):
        return None, (404, {'error': 'Conversation not found'})

    try:
        conn = get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute(
                'INSERT INTO messages (conversation_id, role, content) VALUES (?, ?, ?)',
                (conversation_id, 'user', content),
            )
            user_id = cursor.lastrowid

            assistant_content = get_answer(conversation_id, content)
            cursor.execute(
                'INSERT INTO messages (conversation_id, role, content) VALUES (?, ?, ?)',
                (conversation_id, 'assistant', assistant_content),
            )
            assistant_id = cursor.lastrowid

            conn.commit()
        finally:
            conn.close()

        return {
            'user_message': {'id': user_id, 'role': 'user', 'content': content},
            'assistant_message': {'id': assistant_id, 'role': 'assistant', 'content': assistant_content},
        }, None
    except Exception:
        return None, (500, {'error': 'Internal server error'})


def rename_conversation(conversation_id: str, title: str):
    if not _conversation_exists(conversation_id):
        return None, (404, {'error': 'Conversation not found'})

    execute_query(
        'UPDATE conversations SET conversation_title = ? WHERE id = ?',
        (title, conversation_id),
    )
    return {'message': 'Conversation updated'}, None


def delete_conversation(conversation_id: str):
    if not _conversation_exists(conversation_id):
        return None, (404, {'error': 'Conversation not found'})

    execute_query('DELETE FROM conversations WHERE id = ?', (conversation_id,))
    return {'message': 'Conversation deleted'}, None


def delete_guest_data(guest_id: str):
    rows = fetch_all(
        'SELECT id FROM conversations WHERE metadata LIKE ?',
        (f'%"guest_id":"{guest_id}"%',),
    )
    if not rows:
        return None, (404, {'error': 'Guest not found'})

    execute_query(
        'DELETE FROM conversations WHERE metadata LIKE ?',
        (f'%"guest_id":"{guest_id}"%',),
    )
    return {'status': 'success'}, None
