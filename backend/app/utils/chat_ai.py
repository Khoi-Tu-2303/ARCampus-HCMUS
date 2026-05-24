from ai.agents.chatbot_pipeline import chat
from typing import Tuple
def get_answer(conversation_id : str, query: str) -> Tuple[str, dict]:
    try:
        return chat(conversation_id, query)
    except:
        return 'Đã xảy ra lỗi vui lòng thử lại sau !'
