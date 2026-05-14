import numpy as np
from ai.intent.core.embedder import TextEmbedder
from ai.intent.core.vector_store import VectorStore
from ai.intent.core.config import IntentConfig

class IntentClassifier:
    def __init__(self):
        # Vì là Singleton, gọi khởi tạo ở đây sẽ không tạo thêm vùng nhớ mới
        self.embedder = TextEmbedder()
        self.vector_store = VectorStore()
        self.threshold = IntentConfig.SIMILARITY_THRESHOLD

    def predict(self, query: str) -> dict:
        if self.vector_store.vectors is None:
            raise RuntimeError("Vector store is empty. Cannot predict.")

        # 1. Encode user query
        encoded_query = self.embedder.encode(query)

        # 2. Tính Cosine Similarity
        similarities = np.dot(self.vector_store.vectors, encoded_query)

        # 3. Lấy kết quả cao nhất
        best_index = np.argmax(similarities)
        best_score = float(similarities[best_index])

        # 4. Kiểm tra Threshold fallback
        if best_score < self.threshold:
            return {
                "intent": "fallback",
                "confidence": round(best_score, 4),
                "matched_text": None
            }

        return {
            "intent": self.vector_store.intents[best_index],
            "target": self.vector_store.targets[best_index],
            "metadata" : self.vector_store.metadata[best_index],
            "confidence": round(best_score, 4),
            "matched_text": self.vector_store.texts[best_index]
        }
if __name__ == "__main__":
    a = IntentClassifier()
    print(a.predict("hello"))