import numpy as np

_intent_vectors = None
_intent_labels = None
_intent_texts = None


def load_vector_store():

    global _intent_vectors
    global _intent_labels
    global _intent_texts

    if _intent_vectors is None:

        data = np.load(
            "ai/intent/data/intent_vectors.npz",
            allow_pickle=True
        )

        _intent_vectors = data["vectors"]

        _intent_labels = data["labels"]

        _intent_texts = data["texts"]


def get_vectors():
    return _intent_vectors


def get_labels():
    return _intent_labels


def get_texts():
    return _intent_texts