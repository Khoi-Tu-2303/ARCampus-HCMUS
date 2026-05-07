import os
import re

from dotenv import load_dotenv
from huggingface_hub import login
from sentence_transformers import SentenceTransformer

load_dotenv()

HF_TOKEN = os.getenv("HF_TOKEN")

if HF_TOKEN:
    login(HF_TOKEN)

_embedding_model  = None


def load_embedding_model():
    global _embedding_model 

    if _embedding_model  is None:
        _embedding_model  = SentenceTransformer(
            "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
        )


def get_embedding_model():
    if _embedding_model  is None:
        raise RuntimeError(
            "Embedding model not loaded"
        )

    return _embedding_model 


def data_processing(text: str) -> str:

    text = text.lower().strip()

    text = re.sub(
        r"[^\w\sàáạảãâầấậẩẫăằắặẳẵ"
        r"èéẹẻẽêềếệểễ"
        r"ìíịỉĩ"
        r"òóọỏõôồốộổỗơờớợởỡ"
        r"ùúụủũưừứựửữ"
        r"ỳýỵỷỹđ]",
        "",
        text
    )

    text = re.sub(r"\s+", " ", text)

    return text


def encode(text: str):

    processed = data_processing(text)

    model = get_embedding_model()

    embedding = model.encode(
        processed,
        normalize_embeddings=True
    )

    return embedding