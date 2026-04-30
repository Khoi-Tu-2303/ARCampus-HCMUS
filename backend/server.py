from predict_intent_tfidf import predict
from connect_database import get_info_by_intent, update_chatbot_api
import subprocess
import time
import requests
from fastapi import FastAPI
import uvicorn
import threading
import re
from fastapi import FastAPI, HTTPException

app = FastAPI()
OLLAMA_URL = "http://localhost:11434/api/generate"

SYSTEM_INSTRUCTION = """
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
    - Nếu thiếu thông tin thì trả về đúng chuỗi: "Thông tin đang cập nhật."
    - Không kết hợp dữ liệu để tạo thông tin mới ngoài context

    Cách trả lời:
    - Trả lời tự nhiên, thân thiện như sinh viên hỗ trợ sinh viên
    - Ngắn gọn, dễ hiểu
    - Không lặp lại nguyên văn dữ liệu

    Phong cách:
    - Nhẹ nhàng, rõ ràng, giống người thật
    - Không dài dòng
    """
LOCATION_TRIGGERS = {
    "TOA_A": ["tòa a", "toà a", "khu a", "dãy a", "nhà a", "phòng a"],
    
    "TOA_B": ["tòa b", "toà b", "khu b", "dãy b", "nhà b", "phòng b", "hội trường b"],
    
    "TOA_C": ["tòa c", "toà c", "khu c", "dãy c", "nhà c", "phòng c"],
    
    "TOA_D": ["tòa d", "toà d", "khu d", "dãy d", "nhà d", "phòng d"],
    
    "TOA_E": ["tòa e", "toà e", "khu e", "dãy e", "nhà e", "phòng e"],
    
    "TOA_F": ["tòa f", "toà f", "khu f", "dãy f", "nhà f", "phòng f", "thực hành", "phòng máy"], 
    
    "TOA_G": ["tòa g", "toà g", "khu g", "dãy g", "nhà g", "phòng g"],
    
    "NDH": [
        "nhà điều hành", "phòng đào tạo", "công tác sinh viên", "ctsv", 
        "ban giám hiệu", "nộp đơn", "rút hồ sơ", "bảng điểm", "đóng học phí", 
        "giấy xác nhận", "mộc đỏ", "hành chính"
    ],
    
    "NHA_XE_TRUOC": [
        "nhà xe trước", "bãi xe trước", "gửi xe trước", "lấy xe cổng chính"
    ],
    
    "NHA_XE_SAU": [
        "nhà xe sau", "bãi xe sau", "gửi xe sau", "lấy xe cổng sau"
    ],
    
    "NHA_THE_DUC": [
        "nhà thể dục", "thể chất", "thể thao", "nhà thi đấu", "sân bóng", "học thể dục"
    ],
    
    "CANTEEN": [
        "canteen", "căn tin", "căng tin", "nhà ăn", "quán nước", "quán cơm"
    ]
}

@app.post("/chat")
def chat(request : dict):
    """
    request {
        user_id: str (null)
        role: str (null)
        question: str
    }
    
    respone {
        answer: str
        suggested_location: list(str) 
    }
    """
    question = request["question"]
    intents = predict(question)
    infos = [get_info_by_intent(intent) for intent in intents]
    context = "\n".join(infos)
    prompt = f"{SYSTEM_INSTRUCTION}\n\nCONTEXT:\n{context}\n\nCÂU HỎI:\n{question}\n\nTRẢ LỜI:\n"
    try:
        response = requests.post(OLLAMA_URL, json={
            "model": "gemma:2b",
            "prompt": prompt,
            "stream": False
        }, timeout=30)
        response.raise_for_status()
        answer = response.json()["response"]
        
    except requests.exceptions.Timeout:
        raise HTTPException(status_code=504, detail="Model phản hồi quá chậm")
    except requests.exceptions.ConnectionError:
        raise HTTPException(status_code=503, detail="Không kết nối được Ollama")
    except (KeyError, ValueError):
        raise HTTPException(status_code=502, detail="Phản hồi từ model không hợp lệ")
    
    suggested_location = []
    if "thông tin đang cập nhật" not in answer.lower():
        lower_question = question.lower()
        lower_answer = answer.lower()
        for key, words in LOCATION_TRIGGERS.items():
            if any(w in lower_question or w in lower_answer for w in words):
                suggested_location.append(key)
    return {"answer": answer,
            "suggested_location" : suggested_location}

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