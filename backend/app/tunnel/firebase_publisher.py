import logging
import os
from pathlib import Path

logger = logging.getLogger(__name__)

_firebase_app = None


def _init_firebase() -> None:
    """Initialize Firebase Admin SDK once."""
    global _firebase_app
    if _firebase_app is not None:
        return

    try:
        import firebase_admin
        from firebase_admin import credentials
    except ImportError as exc:
        raise RuntimeError(
            "firebase-admin is not installed. Run: pip install firebase-admin"
        ) from exc

    if firebase_admin._apps:
        _firebase_app = firebase_admin.get_app()
        return

    cred_path = Path(os.getenv("FIREBASE_CREDENTIALS_PATH", "firebase-credentials.json")).resolve()

    if not cred_path.exists():
        raise FileNotFoundError(
            f"Firebase credentials not found at: {cred_path}. "
            "Set FIREBASE_CREDENTIALS_PATH or place firebase-credentials.json in the project root."
        )

    cred = credentials.Certificate(cred_path)
    db_url = os.getenv("FIREBASE_DATABASE_URL")
    options = {"databaseURL": db_url} if db_url else {}

    _firebase_app = firebase_admin.initialize_app(cred, options)
    logger.info("Firebase Admin SDK initialized.")


def publish_url_to_firestore(public_url: str) -> None:
    """Update the backend URL in Firestore."""
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
        merge=True,
    )
    logger.info("Firebase Firestore updated: %s/%s -> %s", collection, doc_id, public_url)


def publish_url_to_realtime_db(public_url: str) -> None:
    """Update the backend URL in Realtime Database."""
    _init_firebase()
    from firebase_admin import db

    path = os.getenv("FIREBASE_REALTIME_PATH", "server_config")
    ref = db.reference(path)
    ref.update({"base_url": public_url})
    logger.info("Firebase Realtime DB updated: /%s/base_url -> %s", path, public_url)


def publish_url(public_url: str) -> None:
    """Publish the backend URL to the configured Firebase database."""
    db_type = os.getenv("FIREBASE_DB_TYPE", "firestore").lower()

    if db_type == "realtime":
        publish_url_to_realtime_db(public_url)
    else:
        publish_url_to_firestore(public_url)
