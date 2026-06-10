import firebase_admin
from firebase_admin import credentials, firestore
from typing import List, Dict, Any
from google.cloud.firestore_v1 import FieldFilter


class FirebaseService:
    """
    Singleton service để connect Firebase Firestore
    và query data cho chatbot system
    """

    _instance = None
    _db = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(FirebaseService, cls).__new__(cls)
            cls._instance._initialize()
        return cls._instance

    def _initialize(self):
        """
        Init Firebase app chỉ 1 lần
        """
        try:
            if not firebase_admin._apps:
                cred = credentials.Certificate("key/firebase_key.json")
                firebase_admin.initialize_app(cred)

            self._db = firestore.client()

        except Exception as e:
            raise Exception(f"Firebase init failed: {str(e)}")

    @property
    def db(self):
        return self._db

    def get_multiple_descriptions(self, keys: List[str]) -> List[str]:
        try:
            if not keys:
                return []

            # convert string → DocumentReference
            docs_ref = [
                self.db.collection("description").document(key)
                for key in keys
            ]

            filter_expr = FieldFilter("__name__", "in", docs_ref)
            docs = self.db.collection("description").where(filter=filter_expr).stream()

            data_map: Dict[str, str] = {}

            for doc in docs:
                data_map[doc.id] = doc.to_dict().get("content", "")

            return [data_map.get(key, "") for key in keys]

        except Exception as e:
            print(f"[Firebase Error] get_multiple_descriptions: {e}")
            return []
        
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

                    # duyệt các key nhỏ bên trong document
                    for sub_key in sub_keys:
                        value = data.get(sub_key)
                        if value is None:
                            continue

                        content = str(value).strip()
                        if not content:
                            continue

                        result_items.append({
                            "collection": collection,
                            "document": key,
                            "field": sub_key,
                            "content": content,
                            "source": f"{collection}/{key}#{sub_key}",
                        })

                return result_items

            except Exception as e:
                print(f"[Firebase Error] get_multiple_descriptions_text: {e}")
                return []
            
    def get_description(self, collection: str, key: str, sub_key: str) -> str:
        try:
            doc = self.db.collection(collection).document(key).get()

            if not doc.exists:
                return ""

            data = doc.to_dict()
            return data.get(sub_key, "").strip()

        except Exception as e:
            print(f"[Firebase Error] get_description: {e}")
            return ""

if __name__ == "__main__":
    firebase = FirebaseService()
    result = firebase.get_description(collection="description", key="A001", sub_key='recommend_building')
    print(result)
