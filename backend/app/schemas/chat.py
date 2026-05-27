from pydantic import BaseModel, Field
from typing import Optional, Any


class CreateConversationRequest(BaseModel):
    id: str | int


class CreateConversationResponse(BaseModel):
    id_conversation: str
    title: str

class ConversationItem(BaseModel):
    id_conversation: str
    title: str
    created_at: str
    
class GetConversationsResponse(BaseModel):
    conversations: list[ConversationItem]
    
class ConversationUpdateRequest(BaseModel):
    title: str = Field(min_length=1)

class SendMessageRequest(BaseModel):
    content: str = Field(min_length=1)

class MessageItem(BaseModel):
    id: str                       
    conversation_id: str
    role: str
    content: str
    created_at: str
    metadata: Any

class SendMessageResponse(BaseModel):
    user_message: MessageItem
    assistant_message: MessageItem
    
class GetMessagesResponse(BaseModel):
    messages: list[MessageItem]

