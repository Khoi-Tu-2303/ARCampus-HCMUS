from pydantic import BaseModel, Field
from typing import Optional


class RegisterRequest(BaseModel):
    username: str = Field(min_length=1)
    password: str = Field(min_length=1)
    full_name: str = Field(min_length=1)
    student_id: str = Field(min_length=1)


class LoginRequest(BaseModel):
    username: str = Field(min_length=1)
    password: str = Field(min_length=1)


class UserResponse(BaseModel):
    id: int
    username: str
    user_role: str


class GuestResponse(BaseModel):
    id: str
    username: Optional[str] = None
    user_role: Optional[str] = None
