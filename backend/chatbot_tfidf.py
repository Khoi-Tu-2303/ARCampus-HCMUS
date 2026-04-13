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
            Bạn là trợ lý sinh viên trong trường đại học.
            Nhiệm vụ: 
                - Hỗ trợ tìm đường trong khuôn viên trường
                - Cung cấp thông tin về địa điểm trong trường (chức năng, giờ làm việc, mô tả)
            Quy tắc:
            - Chỉ sử dụng dữ liệu được cung cấp
            - Không suy đoán, không dùng kiến thức bên ngoài
            - Nếu không có thông tin thì trả lời: "Thông tin đang cập nhật"
            - Không lặp lại dữ liệu đầu vào
            - KHÔNG được hỏi lại người dùng dưới mọi hình thức
            - KHÔNG được gợi ý thêm thông tin
            - Trả lời dễ hiểu, tự nhiên bằng tiếng Việt
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

question = "Thư viện mở cửa lúc mấy giờ"
answer = chatbot(question)
print("Question:", question)
print("Answer:", answer)