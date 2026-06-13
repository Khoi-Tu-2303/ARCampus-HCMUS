from version2.chatbot.chatbot_pipeline import chat
from version2.schemas import ChatbotResponse
from typing import Tuple
def get_answer(conversation_id : str, query: str) -> ChatbotResponse:
    return chat(conversation_id, query)
