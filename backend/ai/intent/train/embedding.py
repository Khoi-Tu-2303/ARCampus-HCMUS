import pandas as pd
import numpy as np
from sentence_transformers import SentenceTransformer
import re
from huggingface_hub import login
from dotenv import load_dotenv
import os

def data_processing(text: str) -> str:
    text = text.lower().strip()
    
    # loại bỏ dấu câu
    text = re.sub(r"[^\w\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễ"
                  r"ìíịỉĩòóọỏõôồốộổỗơờớợởỡ"
                  r"ùúụủũưừứựửữỳýỵỷỹđ]", "", text)
    
    # bỏ khoảng trắng dư
    text = re.sub(r"\s+", " ", text)

    return text

# load dataset
if __name__ == "__main__":
    
    load_dotenv()
    login(os.getenv("HF_TOKEN"))
    
    df = pd.read_csv("data/data_v2.csv")

    df["text"] = df["text"].apply(data_processing)

    texts = df["text"].tolist()
    labels = df["label"].tolist()

    # load model
    model = SentenceTransformer("sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")

    # encode tất cả câu hỏi
    embeddings = model.encode(texts, normalize_embeddings=True)

    np.savez(
        "ai/intent/data/intent_vectors.npz",
        texts=texts,
        labels=labels,
        vectors=embeddings
    )

    print("Dataset size:", len(texts))
    print("Embedding shape:", embeddings.shape)



