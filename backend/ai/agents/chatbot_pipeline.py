"""
chatbot_pipeline.py
────────────────────────────────────────────────────────────────
Pipeline xử lý chính của chatbot.

Luồng:
  1. Intent Classification   (classifier.py)
  2. Entity Extraction       (extractor tương ứng)
  3. Load Context Firebase   (conversation history + user info)
  4. Build input_data dict
  5. Agent.run(input_data)   (call LLM bên trong agent)
  6. Save response + return
────────────────────────────────────────────────────────────────
"""

from ai.intent.core.classifier import IntentClassifier
from ai.agents.agent_factory import AgentFactory
from ai.agents.conversation_memory import ConversationMemoryManager
from firebase.firebase_service import FirebaseService
from typing import Tuple

HISTORY_K = 3


class ChatbotPipeline:
    """
    Orchestrator: điều phối toàn bộ luồng xử lý của chatbot.
    """

    def __init__(self, history_k: int = HISTORY_K):
        self.history_k = history_k

        self.classifier = IntentClassifier()

        self.firebase = FirebaseService()
        self.memory = ConversationMemoryManager()


    def process(self, conversation_id: str, message: str) -> Tuple[str, dict]:
        """
        Hàm tích hợp với API.

        Args:
            conversation_id : ID cuộc hội thoại
            message         : tin nhắn của user

        Returns:
            response (str): câu trả lời từ chatbot
        """

        
        classification = self.classifier.predict(message)
        intent: str = classification.get("intent", "general_chat")
        
        keys: list = []
        key = classification.get("target", "")
        if key != "":
            keys.append(key)
            
        matched_text: str = classification.get("matched_text", None)
        print("[DEBUG] [CHATBOTPIPELINE] Intent =", intent)
        print("[DEBUG] [CHATBOTPIPELINE] Mathced text =", matched_text)
        
        if intent in ["schedule_management"]:
            print("[DEBUG] [CHATBOTPIPELINE] Tính năng chưa cập nhật.")
            return "Tính năng chưa cập nhật.", {}
        
        print("[DEBUG] [CHATBOTPIPELINE] Keys = ", keys)
        
        contexts = self.firebase.get_multiple_descriptions_v2(keys)
        print("[DEBUG] [CHATBOTPIPELINE] Contexts = ", contexts)
        
        history   = self.memory.get_history(conversation_id, self.history_k)
        print("[DEBUG] [CHATBOTPIPELINE] History = ", history)

        input_data = {
            "query":     message,
            "keys" :      keys,
            "intent":    intent,
            "contexts":  contexts,
            "history":   history,
            "user_info": None,
        }

        agent    = AgentFactory.get(intent)
        response, metadata = agent.run(input_data)
        print("[DEBUG] [CHATBOTPIPELINE] Response = ", response)
        print("[DEBUG] [CHATBOTPIPELINE] Metadata = ", metadata)
        
        return response, metadata



_pipeline: ChatbotPipeline | None = None


def get_pipeline() -> ChatbotPipeline:
    global _pipeline
    if _pipeline is None:
        _pipeline = ChatbotPipeline()
    return _pipeline


def chat(conversation_id: str, message: str) -> Tuple[str, dict]:
    """
    Hàm tiện ích để gọi từ API layer.

    Usage:
        from ai.chatbot_pipeline import chat
        response = chat(conversation_id="conv_123", message="Thư viện mở cửa mấy giờ?")
    """
    return get_pipeline().process(conversation_id, message)

