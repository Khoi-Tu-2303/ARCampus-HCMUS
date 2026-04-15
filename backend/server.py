from predict_intent_tfidf import predict
from connect_database import get_info_by_intent, update_chatbot_api
import subprocess
import time
import requests
from fastapi import FastAPI
import uvicorn
import threading
import re

app = FastAPI()
OLLAMA_URL = "http://localhost:11434/api/generate"

@app.post("/chat")
def chat(question : str):
    intents = predict(question)
    infos = [get_info_by_intent(intent) for intent in intents]
    context = " ".join(infos)
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
    response = requests.post(
        OLLAMA_URL,
        json={
            "model": "gemma:2b",
            "prompt": prompt,
            "stream": False
        }
    )
    return response.json()["response"]

def start_api():
    uvicorn.run(app, host="0.0.0.0", port=8000)
    
def start_cloudflare():
    process = subprocess.Popen(
        [
            "cloudflared-windows-amd64.exe",
            "tunnel",
            "--url",
            "http://localhost:8000"
        ],
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True
    )

    url = None

    for line in process.stdout:
        print(line.strip())  # vẫn in log

        if "trycloudflare.com" in line:
            match = re.search(r"https://[a-zA-Z0-9\-]+\.trycloudflare\.com", line)
            if match:
                url = match.group(0)
                break

    return url

if __name__ == "__main__":

    print("Starting API server...")

    threading.Thread(target=start_api).start()

    time.sleep(3)

    print("Starting Cloudflare tunnel...")

    url = start_cloudflare()
    if url is None:
        print("Error Cloudflare")
    else:
        update = {"name": "ollama", "url" : url}
        update_chatbot_api(update)
        print("System ready on", url)