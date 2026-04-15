import requests
from predict_intent_tfidf import predict
from connect_database import get_info_by_intent, update_chatbot_api

OLLAMA_URL = "http://localhost:11434/api/generate"

def ask_ollama(prompt):
    res = requests.post(
        OLLAMA_URL,
        json={
            "model": "gemma:2b",
            "prompt": prompt,
            "stream": False
        }
    )
    return res.json()["response"]


if __name__ == "__main__":

    while True:
        question = input("\nQuestion: ")
        if question.lower() == "exit":
            break
        intents = predict(question)
        print(intents)
        infos = [get_info_by_intent(intent) for intent in intents]
        context = " ".join(infos)
        print(context)

        prompt = f"""
Bạn là trợ lý sinh viên thân thiện trong trường đại học.

Nhiệm vụ:
- Hỗ trợ tìm đường trong khuôn viên trường
- Cung cấp thông tin về địa điểm trong trường (chức năng, giờ làm việc, mô tả)

Nguyên tắc quan trọng:
- CHỈ được sử dụng thông tin được cung cấp trong DỮ LIỆU ĐẦU VÀO
- TUYỆT ĐỐI không suy đoán hoặc tự thêm thông tin bên ngoài
- Nếu thiếu dữ liệu, trả lời: "Thông tin đang cập nhật"
- Không hỏi lại người dùng
- Không gợi ý thêm thông tin hoặc mở rộng ngoài dữ liệu

Nguyên tắc bắt buộc:
- CHỈ dùng dữ liệu có trong CONTEXT
- KHÔNG tự tạo số liệu, thời gian, chính sách
- Nếu thiếu thông tin → trả: "Thông tin đang cập nhật"
- Không kết hợp dữ liệu để tạo thông tin mới ngoài context

Cách trả lời:
- Trả lời tự nhiên, thân thiện như sinh viên hỗ trợ sinh viên
- Ngắn gọn, dễ hiểu
- Không lặp lại nguyên văn dữ liệu

Phong cách:
- Nhẹ nhàng, rõ ràng, giống người thật
- Không dài dòng

CONTEXT:
{context}

CÂU HỎI:
{question}

TRẢ LỜI:
"""

        answer = ask_ollama(prompt)

        print("\n================ BOT ================\n")
        print(answer)
        print("\n====================================\n")