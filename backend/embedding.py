import pandas as pd
import numpy as np
from sentence_transformers import SentenceTransformer

def data_processing(text: str) -> str:
    text = text.lower().strip()
    return text

# load dataset
df = pd.read_csv("data/data.csv")

df["text"] = df["text"].apply(data_processing)

texts = df["text"].tolist()
labels = df["label"].tolist()

# load model
model = SentenceTransformer("sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")

# encode tất cả câu hỏi
embeddings = model.encode(texts, normalize_embeddings=True)

print("Dataset size:", len(texts))
print("Embedding shape:", embeddings.shape)

# tạo dictionary chứa embeddings theo label
label_embeddings = {}

for emb, label in zip(embeddings, labels):
    if label not in label_embeddings:
        label_embeddings[label] = []
    label_embeddings[label].append(emb)

# tính mean embedding cho mỗi label
label_centroids = {}

for label, emb_list in label_embeddings.items():
    label_centroids[label] = np.mean(emb_list, axis=0)

labels = list(label_centroids.keys())
vectors = np.array(list(label_centroids.values()))

print(labels)
print(vectors.shape)

np.savez("data/intent_vectors.npz", labels=labels, vectors=vectors)

