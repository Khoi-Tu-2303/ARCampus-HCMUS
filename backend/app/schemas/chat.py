from pydantic import BaseModel, Field
from typing import Optional, Any


class CreateConversationRequest(BaseModel):
    id: str | int


class CreateConversationResponse(BaseModel):
    id: str
    title: str


class ConversationUpdateRequest(BaseModel):
    title: str = Field(min_length=1)


class SendMessageRequest(BaseModel):
    content: str = Field(min_length=1)


class SimpleMessage(BaseModel):
    id: int
    role: str
    content: str
    created_at: str


class ChatMessageResponse(BaseModel):
    id: int
    role: str
    content: str


class SendMessageResponse(BaseModel):
    user_message: ChatMessageResponse
    assistant_message: ChatMessageResponse
