from abc import ABC, abstractmethod
from typing import Any
from typing import Tuple
from datetime import datetime

VIET_DAYS = {
    0: "Thứ 2", 1: "Thứ 3", 2: "Thứ 4",
    3: "Thứ 5", 4: "Thứ 6", 5: "Thứ 7", 6: "Chủ nhật"
}


class BaseAgent(ABC):
    """
    Lớp nền tảng cho tất cả các Agent.
    Mỗi Agent kế thừa class này và override prompt + _handle().
    """

    name: str = "base_agent"
    prompt: str = ""

    def run(self, input_data: dict) -> Tuple[str, dict]:
        """
        Entry point chính.
        input_data gồm:
          - query (str): câu hỏi của user
          - contexts (list[str]): thông tin để chatbot dựa vào
          - history (list[dict]): lịch sử hội thoại
          - user_info (dict | None): thông tin user
        """
        try:
            return self._handle(input_data)
        except Exception as e:
            print(f"[{self.name}] Error: {e}")
            return "Đã xảy ra lỗi, vui lòng thử lại sau."

    @abstractmethod
    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        """Subclass implement logic xử lý thật tại đây."""
        pass

    def build_messages(self, input_data: dict) -> list[dict]:
        """
        Xây dựng danh sách messages theo format Ollama/OpenAI.
        Thứ tự: system → history → user message hiện tại.
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
        """
        Ghép system prompt +  contexts.
        Subclass có thể override nếu cần format phức tạp hơn.

        """
        now = datetime.now()
        time_context = (
            f"Thời gian hiện tại: {VIET_DAYS[now.weekday()]}, "
            f"ngày {now.strftime('%d/%m/%Y')}, "
            f"lúc {now.strftime('%H:%M')}"
        )
        parts = [self.prompt]
        contexts = input_data.get("contexts", [])
        print("[DEBUG] [BUILD_PROMPT] contexts =", contexts)
        contexts = input_data.get("contexts", [])
        if contexts:
            parts.append(f"\nThông tin bổ sung để trả lời: {', '.join(contexts)}")
        parts.append(time_context)
        user_info = input_data.get("user_info")
        if user_info:
            parts.append(f"\nThông tin sinh viên: {user_info}")
        print("\n".join(parts))
        return "\n".join(parts)

    