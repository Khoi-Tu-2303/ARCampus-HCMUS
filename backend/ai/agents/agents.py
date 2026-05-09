from ai.agents.base_agent import BaseAgent
from ai.llms.ollama_client import OllamaClient


# ─── Shared LLM client (lazy-loaded singleton pattern) ────────────────────────
_llm: OllamaClient | None = None


def _get_llm() -> OllamaClient:
    global _llm
    if _llm is None:
        _llm = OllamaClient()
    return _llm


# ══════════════════════════════════════════════════════════════════════════════
# FacilityAgent
# ══════════════════════════════════════════════════════════════════════════════
class FacilityAgent(BaseAgent):
    """
    Xử lý các câu hỏi về cơ sở vật chất:
    thư viện, căn tin, sân vận động, tòa nhà, phòng ban, v.v.
    """

    name = "facility_agent"
    prompt = """Bạn là trợ lý chatbot của trường đại học, chuyên trả lời các câu hỏi 
về cơ sở vật chất như thư viện, căn tin, sân vận động, bãi xe, tòa nhà, các phòng ban.

Quy tắc:
- Trả lời ngắn gọn, chính xác, thân thiện.
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật".
- Không bịa đặt thông tin không có trong context."""

    def _handle(self, input_data: dict) -> str:
        messages = self.build_messages(input_data)
        return _get_llm().chat(messages)


# ══════════════════════════════════════════════════════════════════════════════
# AcademicAgent
# ══════════════════════════════════════════════════════════════════════════════
class AcademicAgent(BaseAgent):
    """
    Xử lý câu hỏi học thuật: thời khóa biểu, lịch thi,
    đăng ký học phần, thông tin môn học, giờ học.
    """

    name = "academic_agent"
    prompt = """Bạn là trợ lý chatbot của trường đại học, chuyên hỗ trợ các vấn đề học thuật:
thời khóa biểu, lịch thi, đăng ký học phần, thông tin môn học, ca học, tiết học.

Quy tắc:
- Hướng dẫn sinh viên tra cứu thông tin trên hệ thống nhà trường khi cần.
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật".
- Trả lời ngắn gọn, rõ ràng."""

    def _handle(self, input_data: dict) -> str:
        messages = self.build_messages(input_data)
        return _get_llm().chat(messages)


# ══════════════════════════════════════════════════════════════════════════════
# ScheduleAgent
# ══════════════════════════════════════════════════════════════════════════════
class ScheduleAgent(BaseAgent):
    """
    Xử lý câu hỏi về lịch trình: lịch học, lịch thi,
    lịch sự kiện trường, ngày nghỉ lễ.
    """

    name = "schedule_agent"
    prompt = """Bạn là trợ lý chatbot của trường đại học, chuyên cung cấp thông tin lịch trình:
lịch học theo tuần/kỳ, lịch thi cuối kỳ, lịch sự kiện, ngày nghỉ lễ.

Quy tắc:
- Cung cấp thông tin lịch chính xác và đầy đủ.
- Nhắc nhở sinh viên kiểm tra lại trên cổng thông tin chính thức.
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật"."""

    def _handle(self, input_data: dict) -> str:
        messages = self.build_messages(input_data)
        return _get_llm().chat(messages)


# ══════════════════════════════════════════════════════════════════════════════
# DocumentServiceAgent
# ══════════════════════════════════════════════════════════════════════════════
class DocumentServiceAgent(BaseAgent):
    """
    Xử lý câu hỏi về giấy tờ, thủ tục hành chính:
    giấy xác nhận sinh viên, bảng điểm, điểm rèn luyện, quy trình xin giấy.
    """

    name = "document_service_agent"
    prompt = """Bạn là trợ lý chatbot của trường đại học, chuyên hỗ trợ thủ tục giấy tờ hành chính:
giấy xác nhận sinh viên, bảng điểm học tập, bảng điểm rèn luyện, các thủ tục xin giấy tờ.

Quy tắc:
- Hướng dẫn rõ ràng từng bước thủ tục.
- Nêu rõ thời gian xử lý nếu biết.
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật".
- Hướng dẫn sinh viên liên hệ phòng ban phụ trách nếu cần."""

    def _handle(self, input_data: dict) -> str:
        messages = self.build_messages(input_data)
        return _get_llm().chat(messages)


# ══════════════════════════════════════════════════════════════════════════════
# NavigationAgent
# ══════════════════════════════════════════════════════════════════════════════
class NavigationAgent(BaseAgent):
    """
    Xử lý câu hỏi chỉ đường, vị trí trong khuôn viên trường.
    """

    name = "navigation_agent"
    prompt = """Bạn là trợ lý chatbot của trường đại học, chuyên hỗ trợ chỉ đường và định vị
trong khuôn viên trường: vị trí tòa nhà, phòng học, phòng ban, cơ sở vật chất.

Quy tắc:
- Mô tả đường đi rõ ràng, từng bước.
- Sử dụng các mốc dễ nhận biết (cổng trường, tòa nhà, sân...).
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật"."""

    def _handle(self, input_data: dict) -> str:
        messages = self.build_messages(input_data)
        return _get_llm().chat(messages)


class GeneralAgent(BaseAgent):
    """
    Xử lý các câu hỏi chung, không thuộc intent cụ thể nào,
    hoặc khi classifier trả về 'fallback'.
    """

    name = "general_agent"
    prompt = """Bạn là trợ lý chatbot thân thiện của trường đại học.
Hỗ trợ sinh viên với mọi câu hỏi liên quan đến trường học.

Quy tắc:
- Trả lời nhiệt tình, thân thiện.
- Hướng dẫn sinh viên tới đúng bộ phận/kênh hỗ trợ khi cần.
- Nếu không biết: thành thật nói "Thông tin đang cập nhật" thay vì bịa đặt."""

    def _handle(self, input_data: dict) -> str:
        messages = self.build_messages(input_data)
        return _get_llm().chat(messages)

if __name__ == "__main__":
    input_data = {
        "query": "Thứ 2 tới mình có lịch ở phòng này không?",
        
        "contexts": [
            "Thời khoá biểu: Sáng thứ 2 tiết 3-4 Môn Cấu trúc dữ liệu. Địa điểm học: phòng F102."
        ],
        
        "history": [
            {"role": "user", "content": "Chào bạn, mình là sinh viên mới."},
            {"role": "assistant", "content": "Chào bạn! Mình có thể giúp bạn quản lý thời khoá biểu."}
        ],
        
        "user_info": "Nguyễn Văn A - MSSV: 20210001 - Khoa CNTT",
        
    }
    ag = ScheduleAgent()
    mess = ag.build_messages(input_data)
    # print(mess[0]['content'])
    print(ag._handle(input_data))