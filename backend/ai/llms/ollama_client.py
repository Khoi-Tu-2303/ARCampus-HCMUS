import requests
from typing import Any

OLLAMA_BASE_URL = "http://localhost:11434"
DEFAULT_MODEL   = "gemma:2b"          # đổi model tại đây khi cần


class OllamaClient:
    """
    Thin wrapper quanh Ollama REST API.
    Dễ swap sang OpenAI / Gemini / bất kỳ LLM nào bằng cách
    thay thế class này và giữ nguyên interface.
    """

    def __init__(
        self,
        base_url: str = OLLAMA_BASE_URL,
        model: str = DEFAULT_MODEL,
        timeout: int = 60,
    ):
        self.base_url = base_url.rstrip("/")
        self.model = model
        self.timeout = timeout

    def chat(self, messages: list[dict[str, str]]) -> str:
        """
        Gửi danh sách messages theo format {role, content}
        và trả về nội dung phản hồi từ LLM.
        """
        payload: dict[str, Any] = {
            "model": self.model,
            "messages": messages,
            "stream": False,
        }

        try:
            response = requests.post(
                f"{self.base_url}/api/chat",
                json=payload,
                timeout=self.timeout,
            )
            response.raise_for_status()
            data = response.json()
            return data["message"]["content"]

        except requests.exceptions.ConnectionError:
            print("[OllamaClient] Không kết nối được Ollama server.")
            return "Dịch vụ AI tạm thời không khả dụng, vui lòng thử lại sau."
        except requests.exceptions.Timeout:
            print("[OllamaClient] Request timeout.")
            return "Phản hồi mất quá nhiều thời gian, vui lòng thử lại."
        except Exception as e:
            print(f"[OllamaClient] Lỗi: {e}")
            return "Đã xảy ra lỗi khi xử lý yêu cầu."
