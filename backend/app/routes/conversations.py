from fastapi import APIRouter, Query, status
from fastapi.responses import JSONResponse

from app.schemas.chat import (
    ConversationUpdateRequest,
    CreateConversationRequest,
    CreateConversationResponse,
    GetConversationsResponse,
    GetMessagesResponse,
    SendMessageRequest,
    SendMessageResponse,
)
from app.services.conversation_service import (
    create_conversation,
    delete_conversation,
    delete_guest_data,
    get_conversations,
    get_messages,
    rename_conversation,
    send_message,
)

router = APIRouter(prefix="/api", tags=["conversations"])


@router.post(
    "/conversations",
    status_code=status.HTTP_201_CREATED,
    response_model=CreateConversationResponse,
)
def api_create_conversation(payload: CreateConversationRequest):
    data, error = create_conversation(payload.id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.get("/conversations", response_model=GetConversationsResponse)
def api_get_conversations(id: str = Query(...)):
    data, _ = get_conversations(id)
    return data


@router.get(
    "/conversations/{conversation_id}/messages",
    response_model=GetMessagesResponse,
)
def api_get_messages(conversation_id: str):
    data, error = get_messages(conversation_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.post(
    "/conversations/{conversation_id}/messages",
    response_model=SendMessageResponse,
)
def api_send_message(conversation_id: str, payload: SendMessageRequest):
    data, error = send_message(conversation_id, payload.content)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.patch("/conversations/{conversation_id}")
def api_rename_conversation(conversation_id: str, payload: ConversationUpdateRequest):
    data, error = rename_conversation(conversation_id, payload.title)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.delete("/conversations/{conversation_id}")
def api_delete_conversation(conversation_id: str):
    data, error = delete_conversation(conversation_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.delete("/guest/{guest_id}")
def api_delete_guest(guest_id: str):
    data, error = delete_guest_data(guest_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data
