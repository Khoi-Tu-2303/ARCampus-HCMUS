from sentence_transformers import SentenceTransformer
from ai.intent.core.config import IntentConfig
from ai.intent.core.preprocessor import TextPreprocessor

class TextEmbedder:
    """Singleton Class để load model MiniLM đúng 1 lần vào RAM"""
    _instance = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(TextEmbedder, cls).__new__(cls)
            cls._instance._initialize()
        return cls._instance

    def _initialize(self):
        print(f"Loading embedding model: {IntentConfig.MODEL_NAME}...")
        self.model = SentenceTransformer(IntentConfig.MODEL_NAME)
        print("Model loaded successfully.")

    def encode(self, text: str, normalize: bool = True):
        processed_text = TextPreprocessor.clean(text)
        return self.model.encode(processed_text, normalize_embeddings=normalize)
    
    def encode_batch(self, texts: list, normalize: bool = True):
        processed_texts = [TextPreprocessor.clean(t) for t in texts]
        return self.model.encode(processed_texts, normalize_embeddings=normalize)