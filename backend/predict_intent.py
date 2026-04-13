import numpy as np
from sentence_transformers import SentenceTransformer
import re

def data_processing(text: str) -> str:
    text = text.lower().strip()
    
    # loại bỏ dấu câu
    text = re.sub(r"[^\w\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễ"
                  r"ìíịỉĩòóọỏõôồốộổỗơờớợởỡ"
                  r"ùúụủũưừứựửữỳýỵỷỹđ]", "", text)
    
    # bỏ khoảng trắng dư
    text = re.sub(r"\s+", " ", text)

    return text

# load model
model = SentenceTransformer("sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")

# load intent vectors
data = np.load("data/intent_vectors.npz", allow_pickle=True)

labels = data["labels"]
vectors = data["vectors"]  # shape (num_intents, 384)

def predict_intent(query, top_k=3):
    query = data_processing(query)
    query_emb = model.encode([query], normalize_embeddings=True)

    # cosine similarity (do embeddings đã normalize)
    scores = query_emb @ vectors.T

    # lấy top-k
    top_indices = np.argsort(scores[0])[::-1][:top_k]

    results = []
    for idx in top_indices:
        results.append(labels[idx])

    return results

print(predict_intent("Thư viện ở đâu vậy"))
