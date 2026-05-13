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
    prompt = """Bạn là trợ lý chatbot của trường đại học Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh, chuyên trả lời các câu hỏi 
về cơ sở vật chất như thư viện, căn tin, sân vận động, bãi xe, tòa nhà, các phòng ban.

Quy tắc:
- Trả lời chính xác câu hỏi, không hỏi thêm, không trả lời dư thừa (ví dụ: câu trả hỏi về thư viện. Câu trả lời chỉ có thông tin của thư viện).
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
    prompt = """Bạn là trợ lý chatbot của trường Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh, chuyên hỗ trợ các vấn đề học thuật:
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
    prompt = """Bạn là trợ lý chatbot của trường Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh, chuyên cung cấp thông tin lịch trình:
lịch học theo tuần/kỳ, lịch thi cuối kỳ, lịch sự kiện, ngày nghỉ lễ.

Quy tắc:
- Cung cấp thông tin lịch chính xác và đầy đủ.
- Nếu có thông tin do sinh viên cung cấp trong lịch sử trò chuyện, có thể dùng để trả lời nhưng phải nhắc nhở kiểm tra lại trên cổng thông tin chính thức.
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
    prompt = """Bạn là trợ lý chatbot của trường Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh, chuyên hỗ trợ thủ tục giấy tờ hành chính:
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
    prompt = """Bạn là trợ lý chatbot của trường Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh, chuyên hỗ trợ chỉ đường và định vị
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
    prompt = """Bạn là trợ lý chatbot thân thiện của trường Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh.
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
        # "query": "Tôi muốn mượn sách thì mượn ở đâu?",
        # "query": "Phòng công tác sinh viên ở đâu?",
        # "query": "Không biết phòng công tác sinh viên có link website không?",
        "query": "Nhà vệ sinh trong tòa C ở đâu?",
        
        "contexts": [
            # "Tòa A có các phòng thí nghiệm của các Khoa Vật Lý, Hóa học, Môi Trường, Địa Chất và Sinh Học. Mở cửa vào các giờ thực hành theo thời khóa biểu.",
            # "Tòa B có phòng tự học, và hội trường dùng cho các hoạt động chuyên đề. "
            # "Thư viện nằm ở lầu 1 dãy C. Mở cửa từ Thứ 2 - Thứ 7, 7h30 đến 16h. Khi vào cần mang thẻ sinh viên. Mỗi sinh viên mượn tối đa 5 cuốn sách. Thời gian mượn là 3 tuần, và bạn có thể gia hạn thêm 1 tuần. Số lần gia hạn tối đa là 2 lần.",
            # "Sinh viên đăng ký môn học theo lịch của phòng đào tạo trên Portal. Đăng ký ở mục Đăng ký học phần. Link: https://portal.hcmus.edu.vn/",
            # "Phòng công tác sinh viên: chức năng tham mưu và giúp Hiệu trưởng xây dựng các kế hoạch, biện pháp tổ chức thực hiện các hoạt động nhằm giáo dục về chính trị, tư tưởng cho sinh viên; xây dựng các quy chế, quy định và kế hoạch tổ chức quản lý sinh viên; phục vụ quyền lợi chính đáng của sinh viên; tổ chức giám sát, kiểm tra, đánh giá kết quả thực hiện công tác sinh viên theo nhiệm vụ được giao. Vị trí: Nằm ở tầng 2 của Nhà điều hành. Thời gian mở cửa: từ Thứ 2 - Thứ 7, 7h30 - 16h. Email: congtacsinhvien@hcmus.edu.vn. Link: https://hcmus.edu.vn/phong-cong-tac-sinh-vien/"
            "Hầu hết các tòa nhà trong campus đều bố trí nhà vệ sinh ở mỗi tầng. Bạn có thể tìm thấy ở gần khu vực cầu thang hoặc cuối hành lang. Tòa a: Thông tin đang được cập nhật. Tòa b: Thông tin đang được cập nhật. Tòa c: Nhà vệ sinh nằm ở dưới tầng hầm tòa C. Tòa d: Nhà vệ sinh nằm tại lầu 2 của tòa nhà. Tòa e: Cầu thang số 1: có tại tầng 1 và tầng 3. Cầu thang số 2: có tại tầng hầm và tầng 3. Tòa f: Có nhà vệ sinh ở các tầng. Tòa g: Có nhà vệ sinh ở các tầng.",
            "Tòa nhà F bao gồm 4 tầng (tầng hầm và 3 tầng lầu), sở hữu cơ sở vật chất tiêu chuẩn gồm thang bộ, khu vực vệ sinh, kho và phòng trực kỹ thuật. Tòa nhà được thiết kế chuyên biệt cho mục đích đào tạo lý thuyết, với không gian chủ yếu dành cho hệ thống các phòng học được phân bổ dày đặc ở cả 3 tầng nổi, kèm theo khu vực phòng giáo viên."        
        ],
        
        "history": [
            {"role": "user", "content": "Chào bạn, mình là sinh viên mới. Hãy giúp mình tìm hiểu về các tòa nhà trong trường."},
            # {"role": "assistant", "content": "Chào bạn! Mình có thể giúp bạn quản lý thời khoá biểu."}
            
        ],
        
        "user_info": "Nguyễn Văn A - MSSV: 20210001 - Khoa CNTT",
        
    }
    ag = FacilityAgent()
    mess = ag.build_messages(input_data)
    print(mess[0]['content'])
    print("-"*7)
    print(ag._handle(input_data))