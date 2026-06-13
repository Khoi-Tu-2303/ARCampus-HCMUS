import logging
from typing import Any

import requests

logger = logging.getLogger(__name__)

OLLAMA_BASE_URL = "http://localhost:11434"
DEFAULT_MODEL = "qwen2.5:3b"


class OllamaClient:
    """Small wrapper around the Ollama chat API."""

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
            logger.warning("Cannot connect to the Ollama server.")
            return "Dịch vụ AI tạm thời không khả dụng, vui lòng thử lại sau."
        except requests.exceptions.Timeout:
            logger.warning("Ollama request timed out.")
            return "Phản hồi mất quá nhiều thời gian, vui lòng thử lại."
        except Exception:
            logger.exception("Failed to process Ollama request.")
            return "Đã xảy ra lỗi khi xử lý yêu cầu."

    def warmup(self) -> None:
        """Validate that Ollama is reachable and the configured model can answer."""
        show_response = requests.post(
            f"{self.base_url}/api/show",
            json={"model": self.model},
            timeout=min(self.timeout, 15),
        )
        show_response.raise_for_status()

        chat_response = requests.post(
            f"{self.base_url}/api/chat",
            json={
                "model": self.model,
                "messages": [{"role": "user", "content": "ping"}],
                "stream": False,
                "options": {"num_predict": 1},
            },
            timeout=self.timeout,
        )
        chat_response.raise_for_status()
