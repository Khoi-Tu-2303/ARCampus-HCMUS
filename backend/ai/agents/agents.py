from ai.agents.base_agent import BaseAgent
from ai.llms.ollama_client import OllamaClient
from typing import Tuple


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
class InformAgent(BaseAgent):
    """
    Xử lý các câu hỏi về cơ sở vật chất:
    thư viện, căn tin, sân vận động, tòa nhà, phòng ban, v.v.
    """

    name = "facility_agent"
    prompt = """Bạn là trợ lý chatbot của trường đại học Đại học Khoa học Tự nhiên - Thành phố Hồ Chí Minh (HCMUS), chuyên trả lời các câu hỏi 
về cơ sở vật chất như thư viện, căn tin, sân vận động, bãi xe, tòa nhà, các phòng học.

Quy tắc:
- Trả lời chính xác câu hỏi, không hỏi thêm, không trả lời dư thừa.
- Nếu thiếu thông tin cụ thể: trả lời "Thông tin đang cập nhật".
- Không bịa đặt thông tin không có trong context.
- Chỉ trả lời bằng tiếng Việt. Không dùng bất kỳ ngôn ngữ nào khác."""

    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        messages = self.build_messages(input_data)
        metadata = {}
        return _get_llm().chat(messages), metadata

# ══════════════════════════════════════════════════════════════════════════════
# NavigationAgent
# ══════════════════════════════════════════════════════════════════════════════
class NavigationAgent(BaseAgent):
    """
    Xử lý câu hỏi chỉ đường, vị trí trong khuôn viên trường.
    """

    name = "navigation_agent"
    prompt = """Bạn là trợ lý chatbot của trường Đại học Khoa học Tự nhiên - ĐHQG TP.HCM, hỗ trợ sinh viên tìm vị trí phòng học, phòng ban và các địa điểm trong khuôn viên trường. Nhiệm vụ của bạn là mô tả vị trí của địa điểm sinh yêu cầu.

    ## Nguyên tắc:
    - Chỉ trả lời dựa trên thông tin trong context được cung cấp, không tự suy đoán.
    - Không bịa đặt tên phòng, tòa nhà, số tầng.
    - Nếu context không có thông tin → trả lời đúng một câu: "Thông tin hiện đang được cập nhật, bạn vui lòng liên hệ bảo vệ hoặc văn phòng khoa để được hỗ trợ."
    ## Cách trả lời khi có thông tin:
    - Trả lời ngắn gọn cần đi đến toà nhà nào.
    - Chỉ trả lời bằng tiếng Việt. Không dùng bất kỳ ngôn ngữ nào khác.
    """
    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        messages = self.build_messages(input_data)
        metadata = {}
        metadata['recommend_target'] = input_data['recommend_building'][0] if len(input_data['recommend_building']) > 0 else ""
        return _get_llm().chat(messages), metadata


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
- Nếu không biết: thành thật nói "Thông tin đang cập nhật" không bịa đặt.
- Không sử dụng emoji và các ký tự không phải tiếng Việt
- Chỉ trả lời bằng tiếng Việt, trả lời "Tính năng không được hỗ trợ" nếu được yêu cầu trả lời bằng ngôn ngữ khác."""


    def _handle(self, input_data: dict) -> Tuple[str, dict]:
        messages = self.build_messages(input_data)
        metadata = {}
        return _get_llm().chat(messages), metadata
    
if __name__ == "__main__":
    pass
    # input_data = {
    #     # "query": "Tôi muốn mượn sách thì mượn ở đâu?",
    #     # "query": "Phòng công tác sinh viên ở đâu?",
    #     # "query": "Không biết phòng công tác sinh viên có link website không?",
    #     "query": "Hello",
        
    #     "contexts": [
    #         # "Tòa A có các phòng thí nghiệm của các Khoa Vật Lý, Hóa học, Môi Trường, Địa Chất và Sinh Học. Mở cửa vào các giờ thực hành theo thời khóa biểu.",
    #         # "Tòa B có phòng tự học, và hội trường dùng cho các hoạt động chuyên đề. "
    #         # "Thư viện nằm ở lầu 1 dãy C. Mở cửa từ Thứ 2 - Thứ 7, 7h30 đến 16h. Khi vào cần mang thẻ sinh viên. Mỗi sinh viên mượn tối đa 5 cuốn sách. Thời gian mượn là 3 tuần, và bạn có thể gia hạn thêm 1 tuần. Số lần gia hạn tối đa là 2 lần.",
    #         # "Sinh viên đăng ký môn học theo lịch của phòng đào tạo trên Portal. Đăng ký ở mục Đăng ký học phần. Link: https://portal.hcmus.edu.vn/",
    #         # "Phòng công tác sinh viên: chức năng tham mưu và giúp Hiệu trưởng xây dựng các kế hoạch, biện pháp tổ chức thực hiện các hoạt động nhằm giáo dục về chính trị, tư tưởng cho sinh viên; xây dựng các quy chế, quy định và kế hoạch tổ chức quản lý sinh viên; phục vụ quyền lợi chính đáng của sinh viên; tổ chức giám sát, kiểm tra, đánh giá kết quả thực hiện công tác sinh viên theo nhiệm vụ được giao. Vị trí: Nằm ở tầng 2 của Nhà điều hành. Thời gian mở cửa: từ Thứ 2 - Thứ 7, 7h30 - 16h. Email: congtacsinhvien@hcmus.edu.vn. Link: https://hcmus.edu.vn/phong-cong-tac-sinh-vien/"
    #         "Hầu hết các tòa nhà trong campus đều bố trí nhà vệ sinh ở mỗi tầng. Bạn có thể tìm thấy ở gần khu vực cầu thang hoặc cuối hành lang. Tòa a: Thông tin đang được cập nhật. Tòa b: Thông tin đang được cập nhật. Tòa c: Nhà vệ sinh nằm ở dưới tầng hầm tòa C. Tòa d: Nhà vệ sinh nằm tại lầu 2 của tòa nhà. Tòa e: Cầu thang số 1: có tại tầng 1 và tầng 3. Cầu thang số 2: có tại tầng hầm và tầng 3. Tòa f: Có nhà vệ sinh ở các tầng. Tòa g: Có nhà vệ sinh ở các tầng.",
    #         "Tòa nhà F bao gồm 4 tầng (tầng hầm và 3 tầng lầu), sở hữu cơ sở vật chất tiêu chuẩn gồm thang bộ, khu vực vệ sinh, kho và phòng trực kỹ thuật. Tòa nhà được thiết kế chuyên biệt cho mục đích đào tạo lý thuyết, với không gian chủ yếu dành cho hệ thống các phòng học được phân bổ dày đặc ở cả 3 tầng nổi, kèm theo khu vực phòng giáo viên."        
    #     ],
        
    #     "history": [
    #         {"role": "user", "content": "Chào bạn, mình là sinh viên mới. Hãy giúp mình tìm hiểu về các tòa nhà trong trường."},
    #         # {"role": "assistant", "content": "Chào bạn! Mình có thể giúp bạn quản lý thời khoá biểu."}
            
    #     ],
        
    #     "user_info": "Nguyễn Văn A - MSSV: 20210001 - Khoa CNTT",
        
    # }
    # ag = GeneralAgent()
    # mess = ag.build_messages(input_data)
    # print(mess[0]['content'])
    # print("-"*7)
    # print(ag._handle(input_data))