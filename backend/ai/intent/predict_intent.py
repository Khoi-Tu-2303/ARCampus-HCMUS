from embeddings.embedder import load_embedding_model, encode
from embeddings.load_vectors import load_vector_store, get_texts, get_labels, get_vectors
import numpy as np

load_embedding_model()
load_vector_store()

SIMILARITY_THRESHOLD = 0.55


def predict_intent(query: str) -> dict:

    # Encode user query
    encoded_query = encode(query)

    # Load vectors/texts/labels from RAM
    vectors = get_vectors()
    texts = get_texts()
    labels = get_labels()

    # Cosine similarity
    # Vì embedding đã normalize nên dot product = cosine similarity
    similarities = np.dot(
        vectors,
        encoded_query
    )

    # Lấy index có similarity lớn nhất
    best_index = np.argmax(similarities)

    # Score cao nhất
    best_score = float(
        similarities[best_index]
    )

    # Intent tương ứng
    predicted_intent = labels[best_index]

    # Text match gần nhất
    matched_text = texts[best_index]

    # Threshold fallback
    if best_score < SIMILARITY_THRESHOLD:

        return {
            "intent": "fallback",
            "confidence": best_score,
            "matched_text": None
        }

    return {
        "intent": predicted_intent,
        "confidence": best_score,
        "matched_text": matched_text
    }


if __name__ == "__main__":

    while True:

        query = input("User: ")

        result = predict_intent(query)

        print("\nResult:")
        print(result)
        print()