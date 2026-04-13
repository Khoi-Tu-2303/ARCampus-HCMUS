from predict_intent_tfidf import predict
from connect_database import get_info_by_intent
import requests
import os
from dotenv import load_dotenv

load_dotenv()

API_KEY = os.getenv("OPENROUTER_API_KEY")

def chatbot(user_input):
    
    intents = predict(user_input)
    print(intents)
    
    info = [get_info_by_intent(intent) for intent in intents]
    
    context = ' '.join(info)
    print(context)
    
    url = "https://openrouter.ai/api/v1/chat/completions"

    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json"
    }
    payload = {
    "model": "google/gemma-3n-e2b-it:free",
    "messages": [
        {
            "role": "user",
            "content": f"""
            Bạn là trợ lý sinh viên thân thiện trong trường đại học.

            Nhiệm vụ:
            - Hỗ trợ tìm đường trong khuôn viên trường
            - Cung cấp thông tin về địa điểm trong trường (chức năng, giờ làm việc, mô tả)

            Nguyên tắc quan trọng:
            - CHỈ được sử dụng thông tin được cung cấp trong đầu vào
            - TUYỆT ĐỐI không suy đoán hoặc tự thêm thông tin bên ngoài
            - Nếu thiếu dữ liệu, trả lời: "Thông tin đang cập nhật"
            - Không hỏi lại người dùng
            - Không gợi ý thêm thông tin hoặc mở rộng ngoài dữ liệu
            
            Nguyên tắc bắt buộc:
            - CHỈ được dùng thông tin có trong DỮ LIỆU ĐẦU VÀO
            - KHÔNG được tự tạo bất kỳ con số, thời gian, chính sách nào nếu không thấy trong dữ liệu
            - Nếu câu trả lời cần số liệu mà không có trong dữ liệu → trả: "Thông tin đang cập nhật"
            - KHÔNG được kết hợp nhiều mảnh thông tin để tạo ra dữ kiện mới

            Cách trả lời:
            - Trả lời tự nhiên, thân thiện, giống sinh viên đang hỗ trợ sinh viên khác
            - Câu văn ngắn gọn, dễ hiểu, không máy móc
            - Có thể diễn đạt linh hoạt nhưng không thay đổi ý nghĩa dữ liệu gốc
            - Không lặp lại nguyên văn input

            Phong cách:
            - Nhẹ nhàng, rõ ràng, giống người thật đang hướng dẫn
            - Không dài dòng
            Câu hỏi: {user_input}
            Dữ liệu: {context}
            """
        }
    ]
}
    response = requests.post(url, headers=headers, json=payload)

    data = response.json()
    print(data)
    reply = data["choices"][0]["message"]["content"]
    
    return reply

question = "Thư viện có mở buổi tối không"
answer = chatbot(question)
print("Question:", question)
print("Answer:", answer)