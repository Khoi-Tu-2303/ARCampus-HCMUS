from ai.agents.chatbot_pipeline import chat
def get_answer(conversation_id : str, query: str) -> str:
    try:
        return chat(conversation_id, query)
    except:
        return 'Đã xảy ra lỗi vui lòng thử lại sau !'
