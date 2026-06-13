from typing import Tuple

from ai.agents.base_agent import BaseAgent
from ai.llms.ollama_client import OllamaClient

_llm: OllamaClient | None = None


def _get_llm() -> OllamaClient:
    global _llm
    if _llm is None:
        _llm = OllamaClient()
    return _llm


def warmup_llm() -> None:
    _get_llm().warmup()


class InformAgent(BaseAgent):
    """Answer factual questions about campus facilities and services."""

    name = "facility_agent"
    prompt = """Bạn là trợ lý chatbot của Trường Đại học Khoa học Tự nhiên - Đại học Quốc gia Thành phố Hồ Chí Minh (HCMUS), chuyên trả lời các câu hỏi về cơ sở vật chất như thư viện, căn tin, sân vận động, bãi xe, tòa nhà và phòng học.

Quy tắc:
- Trả lời chính xác câu hỏi, không hỏi thêm, không trả lời dư thừa.
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật".
- Không bịa đặt thông tin không có trong context.
- Chỉ trả lời bằng tiếng Việt. Không dùng bất kỳ ngôn ngữ nào khác."""

    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        messages = self.build_messages(input_data)
        metadata = {}
        return _get_llm().chat(messages), metadata


class NavigationAgent(BaseAgent):
    """Answer campus location and navigation questions."""

    name = "navigation_agent"
    prompt = """Bạn là trợ lý chatbot của Trường Đại học Khoa học Tự nhiên - Đại học Quốc gia Thành phố Hồ Chí Minh, hỗ trợ sinh viên tìm vị trí phòng học, phòng ban và các địa điểm trong khuôn viên trường.

Nguyên tắc:
- Chỉ trả lời dựa trên thông tin trong context được cung cấp, không tự suy đoán.
- Không bịa đặt tên phòng, tòa nhà hoặc số tầng.
- Nếu context không có thông tin, trả lời đúng một câu: "Thông tin hiện đang được cập nhật, bạn vui lòng liên hệ bảo vệ hoặc văn phòng khoa để được hỗ trợ."
- Khi có thông tin, chỉ trả lời cần đi đến tòa nhà nào. Không chỉ dẫn đường đi chi tiết.
- Chỉ trả lời bằng tiếng Việt. Không dùng bất kỳ ngôn ngữ nào khác."""

    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        messages = self.build_messages(input_data)
        metadata = {}
        recommend_building = input_data.get("recommend_building")
        if isinstance(recommend_building, list):
            recommend_target = recommend_building[0] if recommend_building else ""
        elif isinstance(recommend_building, str):
            recommend_target = recommend_building
        else:
            recommend_target = ""
        metadata["recommend_target"] = recommend_target
        return _get_llm().chat(messages), metadata


class GeneralAgent(BaseAgent):
    """Answer general school-related questions."""

    name = "general_agent"
    prompt = """Bạn là trợ lý chatbot thân thiện của Trường Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh.
Hỗ trợ sinh viên với các câu hỏi liên quan đến trường học.

Quy tắc:
- Trả lời nhiệt tình, thân thiện.
- Hướng dẫn sinh viên tới đúng bộ phận hoặc kênh hỗ trợ khi cần.
- Nếu không biết: thành thật nói "Thông tin đang cập nhật" và không bịa đặt.
- Không sử dụng emoji hoặc ký tự trang trí.
- Chỉ trả lời bằng tiếng Việt, trả lời "Tính năng không được hỗ trợ" nếu được yêu cầu trả lời bằng ngôn ngữ khác."""

    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        messages = self.build_messages(input_data)
        metadata = {}
        return _get_llm().chat(messages), metadata
