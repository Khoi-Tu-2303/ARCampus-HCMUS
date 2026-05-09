"""
api_integration.py
────────────────────────────────────────────────────────────────
Ví dụ tích hợp pipeline vào FastAPI.
Đặt file này vào backend/api/ hoặc gọi từ router tương ứng.
────────────────────────────────────────────────────────────────
"""

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

from ai.agents.chatbot_pipeline import chat

router = APIRouter(prefix="/chat", tags=["chatbot"])


# ─── Request / Response schema ─────────────────────────────────

class ChatRequest(BaseModel):
    conversation_id: str = Field(..., description="ID cuộc hội thoại")
    message:         str = Field(..., description="Tin nhắn của user")


class ChatResponse(BaseModel):
    conversation_id: str
    response:        str


# ─── Endpoint ──────────────────────────────────────────────────

@router.post("/", response_model=ChatResponse)
async def send_message(req: ChatRequest) -> ChatResponse:
    """
    Nhận tin nhắn, chạy toàn bộ pipeline, trả về phản hồi chatbot.
    """
    if not req.message.strip():
        raise HTTPException(status_code=400, detail="Tin nhắn không được để trống.")

    response = chat(
        conversation_id=req.conversation_id,
        message=req.message,
    )

    return ChatResponse(
        conversation_id=req.conversation_id,
        response=response,
    )
