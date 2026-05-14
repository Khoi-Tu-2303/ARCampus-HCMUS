import os
from dataclasses import dataclass

BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
#ARCampus-HCMUS\backend\ai\intent

@dataclass
class IntentConfig:
    MODEL_NAME: str = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
    DATA_DIR: str = os.path.join(BASE_DIR, "data")
    TXT_PATH: str = os.path.join(DATA_DIR, "data_multilabel.txt")
    CSV_PATH: str = os.path.join(DATA_DIR, "data_multilabel.csv")
    VECTOR_PATH: str = os.path.join(DATA_DIR, "multilabel_vectors.npz")
    SIMILARITY_THRESHOLD: float = 0.55

if __name__ == "__main__":
    print(BASE_DIR)