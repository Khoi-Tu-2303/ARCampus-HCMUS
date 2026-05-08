import hashlib
import hmac
import os
import secrets


def hash_password(password: str) -> str:
    salt = secrets.token_hex(16)
    derived = hashlib.pbkdf2_hmac(
        'sha256',
        password.encode('utf-8'),
        salt.encode('utf-8'),
        120_000,
    ).hex()
    return f'{salt}${derived}'


def verify_password(password: str, stored_hash: str) -> bool:
    try:
        salt, derived = stored_hash.split('$', 1)
        computed = hashlib.pbkdf2_hmac(
            'sha256',
            password.encode('utf-8'),
            salt.encode('utf-8'),
            120_000,
        ).hex()
        return hmac.compare_digest(computed, derived)
    except Exception:
        return False
