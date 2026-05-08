from fastapi import APIRouter, status
from fastapi.responses import JSONResponse

from app.schemas.auth import RegisterRequest, LoginRequest
from app.services.auth_service import register_user, login_user
from app.utils.guest import create_guest_id

router = APIRouter(prefix='/api', tags=['auth'])


@router.post('/register', status_code=status.HTTP_201_CREATED)
def register(payload: RegisterRequest):
    # Đăng ký tài khoản mới.
    data, error = register_user(payload.username, payload.password, payload.full_name, payload.student_id)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.post('/login')
def login(payload: LoginRequest):
    # Đăng nhập bằng username/password.
    data, error = login_user(payload.username, payload.password)
    if error:
        code, body = error
        return JSONResponse(status_code=code, content=body)
    return data


@router.post('/guest')
def guest_login():
    # Tạo phiên guest không lưu vào users.
    return {'id': create_guest_id(), 'username': None}
