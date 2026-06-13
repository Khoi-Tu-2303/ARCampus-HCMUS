from fastapi import APIRouter, status
from fastapi.responses import JSONResponse

from app.schemas.auth import GuestResponse, LoginRequest, RegisterRequest, UserResponse
from app.services.auth_service import login_user, register_user
from app.utils.guest import create_guest_id

router = APIRouter(prefix="/api", tags=["auth"])


@router.post("/register", status_code=status.HTTP_201_CREATED, response_model=UserResponse)
def register(payload: RegisterRequest):
    data, error = register_user(
        payload.username,
        payload.password,
        payload.full_name,
        payload.student_id,
    )
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.post("/login", response_model=UserResponse)
def login(payload: LoginRequest):
    data, error = login_user(payload.username, payload.password)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.post("/guest", response_model=GuestResponse)
def guest_login():
    return {
        "id": create_guest_id(),
        "username": None,
        "user_role": "guest",
    }
