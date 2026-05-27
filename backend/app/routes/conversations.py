from fastapi import APIRouter, Query, status
from fastapi.responses import JSONResponse

from app.schemas.chat import (
    CreateConversationRequest,
    CreateConversationResponse,
    ConversationUpdateRequest,
    SendMessageRequest,
    SendMessageResponse,
    GetConversationsResponse,
    GetMessagesResponse,
)
from app.services.conversation_service import (
    create_conversation,
    get_conversations,
    get_messages,
    send_message,
    rename_conversation,
    delete_conversation,
    delete_guest_data,
)

router = APIRouter(prefix='/api', tags=['conversations'])


@router.post('/conversations', 
             status_code=status.HTTP_201_CREATED,
             response_model=CreateConversationResponse)
def api_create_conversation(payload: CreateConversationRequest):
    # Tạo conversation mới cho user hoặc guest.
    data, error = create_conversation(payload.id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.get('/conversations', response_model=GetConversationsResponse)
def api_get_conversations(id: str = Query(...)):
    # Lấy danh sách conversations theo user/guest id.
    data, _ = get_conversations(id)
    return data


@router.get(
    '/conversations/{conversation_id}/messages',
    response_model=GetMessagesResponse,
)
def api_get_messages(conversation_id: str):
    # Lấy lịch sử chat của một conversation.
    data, error = get_messages(conversation_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.post(
    '/conversations/{conversation_id}/messages',
    response_model=SendMessageResponse,
)
def api_send_message(conversation_id: str, payload: SendMessageRequest):
    # Lưu user message, tạo mock assistant response, và trả về cả hai message.
    data, error = send_message(conversation_id, payload.content)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.patch('/conversations/{conversation_id}')
def api_rename_conversation(conversation_id: str, payload: ConversationUpdateRequest):
    # Đổi tên conversation.
    data, error = rename_conversation(conversation_id, payload.title)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.delete('/conversations/{conversation_id}')
def api_delete_conversation(conversation_id: str):
    # Xoá conversation và messages liên quan bằng cascade.
    data, error = delete_conversation(conversation_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.delete('/guest/{guest_id}')
def api_delete_guest(guest_id: str):
    # Xoá toàn bộ dữ liệu chat của guest.
    data, error = delete_guest_data(guest_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data
