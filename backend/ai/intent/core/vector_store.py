import numpy as np
from ai.intent.core.config import IntentConfig

class VectorStore:
    """Singleton Class để giữ NPZ array trong RAM, dễ dàng thay bằng FAISS/ChromaDB sau này"""
    _instance = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(VectorStore, cls).__new__(cls)
            cls._instance.vectors = None
            cls._instance.intents = None
            cls._instance.texts = None
            cls._instance.targets = None
            cls._instance.metadata = None
            cls._instance.load()
        return cls._instance

    def load(self):
        try:
            data = np.load(IntentConfig.VECTOR_PATH, allow_pickle=True)
            self.vectors = data["vectors"]
            self.intents = data["intents"]
            self.texts = data["texts"]
            self.targets = data["targets"]
            self.metadata = data["metadata"]
            print(f"Loaded {len(self.texts)} vectors from DB.")
        except FileNotFoundError:
            print("Warning: Vector store not found. Please run trainer.py first.")

    def reload(self):
        """Hàm này rất hữu ích cho API. Khi có data mới, gọi API /reload để load lại data mà không cần restart server"""
        print("Reloading vector store...")
        self.load()
if __name__ == "__main__":
    pass