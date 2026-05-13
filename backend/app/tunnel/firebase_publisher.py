"""
Firebase Publisher
- Khởi tạo Firebase Admin SDK một lần duy nhất
- Hỗ trợ cả Firestore và Realtime Database
- Tự động chọn theo biến môi trường FIREBASE_DB_TYPE
"""

import logging
import os
from typing import Optional
from pathlib import Path

logger = logging.getLogger(__name__)

# Lazy-init để tránh import nặng khi không cần
_firebase_app = None


def _init_firebase() -> None:
    """Khởi tạo Firebase Admin SDK (idempotent)."""
    global _firebase_app
    if _firebase_app is not None:
        return

    try:
        import firebase_admin
        from firebase_admin import credentials
    except ImportError:
        raise RuntimeError(
            "firebase-admin chưa được cài. Chạy: pip install firebase-admin"
        )

    if firebase_admin._apps:
        _firebase_app = firebase_admin.get_app()
        return
    
    cred_path = os.getenv("FIREBASE_CREDENTIALS_PATH", "firebase-credentials.json")

    cred_path = Path(cred_path).resolve()

    if not os.path.exists(cred_path):
        raise FileNotFoundError(
            f"Firebase credentials không tìm thấy tại: {cred_path}\n"
            "Set biến môi trường FIREBASE_CREDENTIALS_PATH hoặc đặt file "
            "firebase-credentials.json ở thư mục gốc."
        )

    cred = credentials.Certificate(cred_path)

    # Realtime DB cần database_url
    db_url = os.getenv("FIREBASE_DATABASE_URL")
    options = {"databaseURL": db_url} if db_url else {}

    _firebase_app = firebase_admin.initialize_app(cred, options)
    logger.info("Firebase Admin SDK initialized.")


def publish_url_to_firestore(public_url: str) -> None:
    """
    Cập nhật URL vào Firestore.

    Document path: FIREBASE_FIRESTORE_COLLECTION / FIREBASE_FIRESTORE_DOC_ID
    Mặc định:      server_config / backend_url
    """
    _init_firebase()
    from firebase_admin import firestore

    collection = os.getenv("FIREBASE_FIRESTORE_COLLECTION", "server_config")
    doc_id = os.getenv("FIREBASE_FIRESTORE_DOC_ID", "backend_url")

    db = firestore.client()
    doc_ref = db.collection(collection).document(doc_id)
    doc_ref.set(
        {
            "base_url": public_url,
            "updated_at": firestore.SERVER_TIMESTAMP,
        },
        merge=True,   # Không xóa các field khác trong document
    )
    logger.info(
        " Firebase Firestore updated: %s/%s → %s", collection, doc_id, public_url
    )


def publish_url_to_realtime_db(public_url: str) -> None:
    """
    Cập nhật URL vào Realtime Database.

    Path: FIREBASE_REALTIME_PATH (mặc định: server_config)
    """
    _init_firebase()
    from firebase_admin import db

    path = os.getenv("FIREBASE_REALTIME_PATH", "server_config")
    ref = db.reference(path)
    ref.update({"base_url": public_url})
    logger.info(
        " Firebase Realtime DB updated: /%s/base_url → %s", path, public_url
    )


def publish_url(public_url: str) -> None:
    """
    Entry point: tự động chọn Firestore hoặc Realtime DB
    dựa trên biến môi trường FIREBASE_DB_TYPE.

    FIREBASE_DB_TYPE=firestore  → dùng Firestore (mặc định)
    FIREBASE_DB_TYPE=realtime   → dùng Realtime Database
    """
    db_type = os.getenv("FIREBASE_DB_TYPE", "firestore").lower()

    if db_type == "realtime":
        publish_url_to_realtime_db(public_url)
    else:
        publish_url_to_firestore(public_url)