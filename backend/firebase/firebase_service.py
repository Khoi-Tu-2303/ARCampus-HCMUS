import logging
from typing import Any, Dict, List

import firebase_admin
from firebase_admin import credentials, firestore

logger = logging.getLogger(__name__)


class FirebaseService:
    """Singleton Firestore service used by the chatbot pipeline."""

    _instance = None
    _db = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(FirebaseService, cls).__new__(cls)
            cls._instance._initialize()
        return cls._instance

    def _initialize(self):
        try:
            if not firebase_admin._apps:
                cred = credentials.Certificate("key/firebase_key.json")
                firebase_admin.initialize_app(cred)

            self._db = firestore.client()
        except Exception as exc:
            raise RuntimeError(f"Firebase init failed: {exc}") from exc

    @property
    def db(self):
        return self._db

    def get_multiple_descriptions_v2(
        self,
        collection: str,
        keys: List[str],
        sub_keys: List[str],
    ) -> List[Dict[str, str]]:
        try:
            if not keys:
                return []

            result_items: List[Dict[str, str]] = []

            for key in keys:
                doc = self.db.collection(collection).document(key).get()

                if not doc.exists:
                    continue

                data: Dict[str, Any] = doc.to_dict() or {}

                for sub_key in sub_keys:
                    value = data.get(sub_key)
                    if value is None:
                        continue

                    content = str(value).strip()
                    if not content:
                        continue

                    result_items.append(
                        {
                            "collection": collection,
                            "document": key,
                            "field": sub_key,
                            "content": content,
                            "source": f"{collection}/{key}#{sub_key}",
                        }
                    )

            return result_items
        except Exception:
            logger.exception("Failed to fetch Firestore descriptions.")
            return []

    def get_description(self, collection: str, key: str, sub_key: str) -> str:
        try:
            doc = self.db.collection(collection).document(key).get()

            if not doc.exists:
                return ""

            data = doc.to_dict() or {}
            return str(data.get(sub_key, "")).strip()
        except Exception:
            logger.exception("Failed to fetch Firestore description.")
            return ""
