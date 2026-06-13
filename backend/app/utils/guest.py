from uuid import uuid4


def create_guest_id() -> str:
    return f'guest_{uuid4().hex[:8]}'
