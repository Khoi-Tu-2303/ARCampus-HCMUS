from abc import ABC, abstractmethod
from typing import Tuple
from datetime import datetime

VIET_DAYS = {
    0: "Thứ 2", 1: "Thứ 3", 2: "Thứ 4",
    3: "Thứ 5", 4: "Thứ 6", 5: "Thứ 7", 6: "Chủ nhật",
}


class BaseAgent(ABC):
    """
    Base class for all chatbot agents.
    Subclasses override prompt and _handle().
    """

    name: str = "base_agent"
    prompt: str = ""

    def run(self, input_data: dict) -> Tuple[str, dict]:
        """
        Entry point.
        input_data includes:
          - query (str)
          - contexts (list[str | dict])
          - history (list[dict])
          - user_info (dict | None)
        """
        try:
            return self._handle(input_data)
        except Exception as e:
            print(f"[{self.name}] Error: {e}")
            return "Đã xảy ra lỗi, vui lòng thử lại sau.", {}

    @abstractmethod
    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        pass

    def build_messages(self, input_data: dict) -> list[dict]:
        """
        Build messages in Ollama/OpenAI format.
        Order: system -> history -> current user message.
        """
        messages = [{"role": "system", "content": self._build_system_prompt(input_data)}]

        for msg in input_data.get("history", []):
            messages.append({
                "role": msg.get("role", "user"),
                "content": msg.get("content", ""),
            })

        messages.append({"role": "user", "content": input_data.get("query", "")})
        return messages

    def _build_system_prompt(self, input_data: dict) -> str:
        now = datetime.now()
        time_context = (
            f"Thời gian hiện tại: {VIET_DAYS[now.weekday()]}, "
            f"ngày {now.strftime('%d/%m/%Y')}, "
            f"lúc {now.strftime('%H:%M')}"
        )

        parts = [
            self.prompt,
            (
                "\nRAG grounding rules:\n"
                "- Chỉ dùng thông tin trong CONTEXT để trả lời các câu hỏi factual.\n"
                "- History chỉ dùng để hiểu câu hỏi follow-up, không dùng history làm nguồn sự thật.\n"
                "- Nếu CONTEXT không có thông tin cần thiết, trả lời \"Thông tin đang cập nhật\"."
            ),
        ]

        context_block = self._format_contexts(input_data.get("contexts", []))
        if context_block:
            parts.append(f"\nCONTEXT:\n{context_block}")

        parts.append(time_context)

        user_info = input_data.get("user_info")
        if user_info:
            parts.append(f"\nThông tin sinh viên: {user_info}")

        return "\n".join(parts)

    def _format_contexts(self, contexts: list) -> str:
        lines = []

        for index, item in enumerate(contexts or [], start=1):
            if isinstance(item, dict):
                source = item.get("source") or f"context#{index}"
                content = str(item.get("content", "")).strip()
                if not content:
                    continue
                lines.append(f"[{source}]\n{content}")
                continue

            content = str(item).strip()
            if content:
                lines.append(f"[context#{index}]\n{content}")

        return "\n\n".join(lines)
