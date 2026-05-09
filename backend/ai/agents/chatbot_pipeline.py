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
from ai.extractor.facility_extractor import FacilityExtractor
from ai.extractor.academic_extractor import AcademicExtractor
from ai.extractor.document_extractor import DocumentServiceExtractor
from ai.agents.agent_factory import AgentFactory
from ai.agents.conversation_memory import ConversationMemoryManager
from firebase.firebase_service import FirebaseService

# ─── Số lượng tin nhắn lịch sử mặc định ──────────────────────
HISTORY_K = 10


class ChatbotPipeline:
    """
    Orchestrator: điều phối toàn bộ luồng xử lý của chatbot.
    """

    def __init__(self, history_k: int = HISTORY_K):
        self.history_k = history_k

        # Step 1 – Intent classifier
        self.classifier = IntentClassifier()

        # Step 2 – Extractors (map intent → extractor instance)
        self._extractors = {
            "facility_query":  FacilityExtractor(),
            "academic_query":  AcademicExtractor(),
            "document_service":  DocumentServiceExtractor(),
        }

        # Step 3 – Memory manager
        self.firebase = FirebaseService()
        self.memory = ConversationMemoryManager()

    # ──────────────────────────────────────────────────────────
    # Public API
    # ──────────────────────────────────────────────────────────

    def process(self, conversation_id: str, message: str) -> str:
        """
        Hàm tích hợp với API.

        Args:
            conversation_id : ID cuộc hội thoại
            message         : tin nhắn của user

        Returns:
            response (str): câu trả lời từ chatbot
        """

        # ── Step 1: Classify intent ────────────────────────────
        classification = self.classifier.predict(message)
        intent: str = classification.get("intent", "fallback")
        print("[DEBUG] [CHATBOTPIPELINE] Intent = ", intent)
        
        # Trường hợp nâng cấp sau
        if intent in ["schedule_management", "navigation"]:
            print("[DEBUG] [CHATBOTPIPELINE] Tính năng chưa cập nhật.")
            return "Tính năng chưa cập nhật ..."
        
        # ── Step 2: Extract keys ───────────────────────────
        extractor = self._extractors.get(intent)
        keys: list[str] = extractor.extract(message) if extractor else []
        print("[DEBUG] [CHATBOTPIPELINE] Keys = ", keys)
        
        # ── Step 3: Load context từ Firebase ──────────────────
        contexts = self.firebase.get_multiple_descriptions(keys)
        print("[DEBUG] [CHATBOTPIPELINE] Contexts = ", contexts)
        
        # ── Step 3: Load History Conversation từ SQLite ──────────────────
        history   = self.memory.get_history(conversation_id, self.history_k)
        print("[DEBUG] [CHATBOTPIPELINE] History = ", history)

        # ── Step 4: Build input_data ───────────────────────────
        input_data = {
            "query":     message,
            "intent":    intent,
            "contexts":  contexts,
            "history":   history,
            "user_info": None,
        }

        # ── Step 5: Route đến Agent → Call LLM ─────────────────
        agent    = AgentFactory.get(intent)
        response = agent.run(input_data)
        print("[DEBUG] [CHATBOTPIPELINE] Response = ", response)
        
        return response


# ─── Module-level convenience function ────────────────────────────────────────

_pipeline: ChatbotPipeline | None = None


def get_pipeline() -> ChatbotPipeline:
    global _pipeline
    if _pipeline is None:
        _pipeline = ChatbotPipeline()
    return _pipeline


def chat(conversation_id: str, message: str) -> str:
    """
    Hàm tiện ích để gọi từ API layer.

    Usage:
        from ai.chatbot_pipeline import chat
        response = chat(conversation_id="conv_123", message="Thư viện mở cửa mấy giờ?")
    """
    return get_pipeline().process(conversation_id, message)

