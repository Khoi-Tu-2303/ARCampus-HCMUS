import firebase_admin
from firebase_admin import credentials, firestore
from typing import List, Dict
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
        
if __name__ == "__main__":
    firebase = FirebaseService()

    keys = [
        "library",
        "canteen",
        "parking"
    ]

    result = firebase.get_multiple_descriptions(keys)

    print("RESULT:")
    for i, r in enumerate(result):
        print(keys[i], "=>", r)