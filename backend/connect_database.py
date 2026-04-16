import firebase_admin
from firebase_admin import credentials
from firebase_admin import firestore

cred = credentials.Certificate("key/firebase_key.json")
firebase_admin.initialize_app(cred)

db = firestore.client()

def get_info_by_intent(intent: str):
    try:
        doc = db.collection("description_intent").document(intent).get()

        if doc.exists:
            return doc.to_dict().get("info")
        else:
            return ""

    except Exception as e:
        return ""

def update_chatbot_api(data: dict):
    try:
        db.collection("api").document("chatbot").set(data, merge=True)
        return True
    except Exception as e:
        print("Error:", e)
        return False
