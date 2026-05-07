import numpy as np
from sentence_transformers import SentenceTransformer
from embedding import data_processing
from huggingface_hub import login
from dotenv import load_dotenv
import os


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

def predict_intent_threshold(query, threshold=0.3):
    query = data_processing(query)
    query_emb = model.encode([query], normalize_embeddings=True)

    scores = query_emb @ vectors.T
    scores = scores[0]

    indices = np.where(scores > threshold)[0]

    results = [labels[idx] for idx in indices]

    return results

def encode_query(model, query):
    return model.encode(query)

def top_k_similar(query_vector, vectors, k=5):
    sims = np.dot(vectors, query_vector)
    top_k_idx = np.argpartition(sims, -k)[-k:]
    top_k_idx = top_k_idx[np.argsort(sims[top_k_idx])[::-1]]
    top_k_scores = sims[top_k_idx]
    return top_k_idx, top_k_scores

# def predict_intent_threshold(labels, top_k_idx, top_k_scores, threshold=0.2)

if __name__ == "__main__":
    
    load_dotenv()
    login(os.getenv("HF_TOKEN"))
    
    # load model
    model = SentenceTransformer("sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")

    # load intent vectors
    data = np.load("data/intent_vectors.npz", allow_pickle=True)

    texts = data["texts"]
    labels = data["labels"]
    vectors = data["vectors"]
    while True:
        query = input("Nhập câu hỏi: ")
        
        vector_query = model.encode(query, normalize_embeddings=True)
        id, score = top_k_similar(query_vector=vector_query,
                                vectors=vectors,
                                k=10)
        for i, s in zip(id, score):
            print(f"Text   : {texts[i]}")
            print(f"Label  : {labels[i]}")
            print(f"Score  : {s:.4f}")
            print("-" * 40)
    
        
