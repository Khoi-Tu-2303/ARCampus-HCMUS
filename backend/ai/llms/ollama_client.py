import requests
from typing import Any

OLLAMA_BASE_URL = "http://localhost:11434"
DEFAULT_MODEL   = "qwen2.5:3b"      # đổi model tại đây khi cần ("llama3.2")      


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

    def warmup(self) -> None:
        """
        Validate that Ollama is reachable and the configured model can answer.
        This method raises on failure so backend startup can fail fast.
        """
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


# class QueryRewriter:

#     def __init__(self, llm_client: OllamaClient):
#         self.llm = llm_client

#     def rewrite(
#         self,
#         history: list[dict[str, str]],
#         current_query: str,
#     ) -> str:

#         history_text = ""

#         for msg in history:
#             role = msg["role"].capitalize()
#             content = msg["content"]

#             history_text += f"{role}: {content}\n"

#         prompt = f"""
# You are a retrieval query rewriting assistant.

# Your task:
# Rewrite the current user query into a standalone query
# using the conversation history.

# IMPORTANT RULES:
# - DO NOT answer the question
# - ONLY rewrite it
# - Resolve pronouns:
#   it, this, that, they, he, she, there
# - Keep original meaning
# - Output ONLY the rewritten query

# Conversation History:
# {history_text}

# Current Query:
# {current_query}

# Rewritten Query:
# """

#         messages = [
#             {
#                 "role": "system",
#                 "content": (
#                     "You rewrite conversational queries "
#                     "into standalone retrieval queries."
#                 )
#             },
#             {
#                 "role": "user",
#                 "content": prompt
#             }
#         ]

#         rewritten_query = self.llm.chat(messages)

#         return rewritten_query.strip()


# # =========================================================
# # TEST
# # =========================================================

# if __name__ == "__main__":

#     ollama_client = OllamaClient(
#         model="qwen2.5:3b"
#     )

#     rewriter = QueryRewriter(
#         llm_client=ollama_client
#     )

#     # -------------------------------
#     # TEST CASE 1
#     # -------------------------------

#     history = [
#         {
#             "role": "user",
#             "content": "Thư viện trường mở cửa lúc mấy giờ?"
#         },
#         {
#             "role": "assistant",
#             "content": "Thư viện mở từ 7h đến 21h."
#         }
#     ]

#     current_query = "Nó nằm ở đâu?"

#     rewritten = rewriter.rewrite(
#         history=history,
#         current_query=current_query
#     )

#     print("=" * 80)
#     print("CURRENT QUERY:")
#     print(current_query)

#     print("\nREWRITTEN QUERY:")
#     print(rewritten)
#     print("=" * 80)

#     # -------------------------------
#     # TEST CASE 2
#     # -------------------------------

#     history = [
#         {
#             "role": "user",
#             "content": "Ngày mai tôi học môn AI ở phòng B203."
#         },
#         {
#             "role": "assistant",
#             "content": "Môn AI bắt đầu lúc 7h30."
#         }
#     ]

#     current_query = "Tôi đi tới đó như thế nào?"

#     rewritten = rewriter.rewrite(
#         history=history,
#         current_query=current_query
#     )

#     print("=" * 80)
#     print("CURRENT QUERY:")
#     print(current_query)

#     print("\nREWRITTEN QUERY:")
#     print(rewritten)
#     print("=" * 80)
