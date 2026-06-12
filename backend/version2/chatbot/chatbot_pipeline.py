from version2.nlu.service import IntentEnityClassifier
from version2.enity_linking.service import EntityLinking
from version2.schemas import IntentType, MatchResult, ConversationTurn, ChatbotResponse
from ai.agents.agents import InformAgent, NavigationAgent, GeneralAgent
from version2.chatbot.history import find_entities_in_history, find_intent_in_history
from firebase.firebase_service import FirebaseService
from ai.agents.conversation_memory import ConversationMemoryManager

HISTORY_K = 3
CONTEXT_PREVIEW_CHARS = 180
CONTEXT_COLLECTION = "campusInfo"
CONTEXT_FIELDS = [
    "ThongBao",
    "GioiThieuChung",
    "QuyDinh",
    "ThoiGian",
    "ThongTinLienLac",
    "ViTri",
]
FIELD_KEYWORDS = [
    (
        "ThoiGian",
        [
            "mo cua",
            "mở cửa",
            "may gio",
            "mấy giờ",
            "thoi gian",
            "thời gian",
            "luc nao",
            "lúc nào",
            "gio",
            "giờ",
        ],
    ),
    (
        "ViTri",
        [
            "o dau",
            "ở đâu",
            "vi tri",
            "vị trí",
            "nam dau",
            "nằm đâu",
            "cho nao",
            "chỗ nào",
            "duong",
            "đường",
            "di toi",
            "đi tới",
        ],
    ),
    (
        "QuyDinh",
        [
            "quy dinh",
            "quy định",
            "can gi",
            "cần gì",
            "duoc khong",
            "được không",
            "co duoc",
            "có được",
            "thu tuc",
            "thủ tục",
        ],
    ),
    (
        "ThongTinLienLac",
        [
            "lien he",
            "liên hệ",
            "email",
            "sdt",
            "sđt",
            "so dien thoai",
            "số điện thoại",
            "website",
        ],
    ),
    (
        "ThongBao",
        [
            "thong bao",
            "thông báo",
            "tin moi",
            "tin mới",
            "cap nhat",
            "cập nhật",
        ],
    ),
]


class ChatbotPipeline:

    def __init__(self, history_k: int = HISTORY_K):
        self.history_k = history_k
        self.classifier = IntentEnityClassifier()
        self.entity_linking = EntityLinking()
        self.firebase = FirebaseService()
        self.inform_agent = InformAgent()
        self.navigation_agent = NavigationAgent()
        self.general_agent = GeneralAgent()
        self.memory = ConversationMemoryManager()
    # ------------------------------------------------------------------
    # Public entry point
    # ------------------------------------------------------------------
    def process(self, conversation_id: str, message: str) -> str:
        nlu_result = self.classifier.predict(message)
        entities_matched = self.entity_linking.predict_batch(nlu_result.entities)

        turn = ConversationTurn(
            user_text=message,
            conversation_id=conversation_id,
            intents=nlu_result.intents,
            entities=entities_matched,
        )

        top_intent = turn.intents[0]

        if top_intent.type == IntentType.NAVIGATION:
            response, matched, metadata, intent = self._handle_navigation(turn)
        elif top_intent.type == IntentType.INFORM:
            response, matched, metadata, intent = self._handle_inform(turn)
        elif top_intent.type == IntentType.GENERAL:
            response, matched, metadata, intent = self._handle_general(turn)
        else:
            response, matched, metadata, intent = self._handle_unknown(turn)

        return ChatbotResponse(
            response=response,
            intent=intent,
            entities=matched,
            metadata=metadata,
        ).to_dict()

    def _handle_navigation(self, turn: ConversationTurn) -> tuple:
        unknown = [e for e in turn.entities if e.status == "unknown"]
        if unknown:
            names = ", ".join(e.entity_text for e in unknown)
            return f"Hiện tại tôi không có thông tin về {names}.", turn.entities, {}, turn.intents[0]

        matched = [e for e in turn.entities if e.status == "matched"]
        if not matched:
            matched = find_entities_in_history(turn.conversation_id)
        if not matched:
            return "Bạn hãy cho tôi biết rõ địa điểm bạn muốn đến!", [], {}, turn.intents[0]
        if len(matched) > 1:
            names = ", ".join(e.entity_text for e in matched)
            return f"Bạn muốn đến địa điểm nào trong số: {names}?", [], {}, turn.intents[0]
        input_data = self._build_input(turn, matched)
        message, metadata = self.navigation_agent.run(input_data)
        metadata = self._with_rag_metadata(metadata, input_data)
        return message, matched, metadata, turn.intents[0]

    def _handle_inform(self, turn: ConversationTurn) -> tuple:
        matched = [e for e in turn.entities if e.status == "matched"]
        if not matched:
            matched = find_entities_in_history(turn.conversation_id)
        if not matched:
            return "Bạn cần thông tin về địa điểm nào?", [], {}, turn.intents[0]
        input_data = self._build_input(turn, matched)
        message, metadata = self.inform_agent.run(input_data)
        metadata = self._with_rag_metadata(metadata, input_data)
        return message, matched, metadata, turn.intents[0]

    def _handle_unknown(self, turn: ConversationTurn) -> tuple:
        if not turn.entities:
            return "Xin lỗi, tôi chỉ hỗ trợ tìm kiếm thông tin hoặc chỉ đường đến các địa điểm trong khuôn viên trường.", [], {}, turn.intents[0]

        history_intents = find_intent_in_history(turn.conversation_id)
        if not history_intents:
            return "Bạn cần hỗ trợ tìm đường hay hỏi thông tin về địa điểm?", turn.entities, {}, turn.intents[0]

        last_intent = history_intents[0].type
        turn.intents = history_intents
        if last_intent == IntentType.NAVIGATION:
            return self._handle_navigation(turn)  # intent = NAVIGATION từ handler
        if last_intent == IntentType.INFORM:
            return self._handle_inform(turn)      # intent = INFORM từ handler

        return "Xin lỗi, tôi chỉ hỗ trợ tìm kiếm thông tin hoặc chỉ đường đến các địa điểm trong khuôn viên trường.", [], {}, turn.intents[0]

    def _handle_general(self, turn: ConversationTurn) -> tuple:
        input_data = {
            "query": turn.user_text,
            "contexts": [],
            "history": [],
            "user_info": None,
        }
        message, metadata = self.general_agent.run(input_data)
        return message, [], metadata, turn.intents[0]
    
    def _build_input(self, turn: ConversationTurn, matched: list[MatchResult]) -> dict:
        keys = [e.matched_id for e in matched if e.matched_id]
        history   = self.memory.get_history(turn.conversation_id, self.history_k)
        context_fields, preferred_fields = self._rank_context_fields(turn.user_text)
        context = self.firebase.get_multiple_descriptions_v2(
            collection=CONTEXT_COLLECTION,
            keys=keys,
            sub_keys=context_fields,
        )
        self._log_retrieved_context(keys, context_fields, context)
        recommend_building = (
            self.firebase.get_description(
                collection=CONTEXT_COLLECTION,
                key=keys[0],
                sub_key='recommend_building',
            )
            if keys
            else ""
        )
        return {
            "query": turn.user_text,
            "contexts": context,
            "history": history,
            "recommend_building" : recommend_building,
            "sources": self._context_sources(context),
            "preferred_context_fields": preferred_fields,
            "user_info": None,
        }

    def _log_retrieved_context(
        self,
        keys: list[str],
        fields: list[str],
        contexts: list,
    ) -> None:
        print("[DEBUG] [RAG] Retrieve collection =", CONTEXT_COLLECTION)
        print("[DEBUG] [RAG] Keys =", keys)
        print("[DEBUG] [RAG] Field order =", fields)
        print("[DEBUG] [RAG] Context count =", len(contexts or []))

        for index, item in enumerate(contexts or [], start=1):
            if isinstance(item, dict):
                source = item.get("source", "")
                content = str(item.get("content", "")).replace("\n", " ").strip()
            else:
                source = f"context#{index}"
                content = str(item).replace("\n", " ").strip()

            if len(content) > CONTEXT_PREVIEW_CHARS:
                content = content[:CONTEXT_PREVIEW_CHARS] + "..."

            print(f"[DEBUG] [RAG] Context {index} [{source}] = {content}")

    def _rank_context_fields(self, query: str) -> tuple[list[str], list[str]]:
        normalized = query.lower()
        preferred_fields: list[str] = []

        for field, keywords in FIELD_KEYWORDS:
            if any(keyword in normalized for keyword in keywords):
                preferred_fields.append(field)

        ordered_fields = list(dict.fromkeys(["ThongBao"] + preferred_fields + [
            field for field in CONTEXT_FIELDS if field not in preferred_fields
        ]))
        return ordered_fields, preferred_fields

    def _context_sources(self, contexts: list) -> list[dict]:
        sources = []
        seen = set()

        for item in contexts or []:
            if not isinstance(item, dict):
                continue

            source = item.get("source")
            if not source or source in seen:
                continue

            seen.add(source)
            sources.append({
                "collection": item.get("collection"),
                "document": item.get("document"),
                "field": item.get("field"),
                "source": source,
            })

        return sources

    def _with_rag_metadata(self, metadata: dict | None, input_data: dict) -> dict:
        metadata = dict(metadata or {})
        contexts = input_data.get("contexts", []) or []
        preferred_fields = input_data.get("preferred_context_fields", []) or []

        warning = None
        if not contexts:
            warning = "empty_context"
        elif preferred_fields:
            context_fields = {
                item.get("field")
                for item in contexts
                if isinstance(item, dict)
            }
            if not any(field in context_fields for field in preferred_fields):
                warning = "weak_context"

        metadata["sources"] = input_data.get("sources", [])
        metadata["grounding"] = {
            "context_available": bool(contexts),
            "warning": warning,
        }
        return metadata
_pipeline: ChatbotPipeline | None = None


def get_pipeline() -> ChatbotPipeline:
    global _pipeline
    if _pipeline is None:
        _pipeline = ChatbotPipeline()
    return _pipeline


def chat(conversation_id: str, message: str) -> str:
    return get_pipeline().process(conversation_id, message)


if __name__ == "__main__":
    while True:
        q = input("q = ")
        result = chat("conv_123", q)
        print(result)
