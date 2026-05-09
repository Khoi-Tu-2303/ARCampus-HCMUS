from ai.intent.core.classifier import IntentClassifier
from ai.intent.core.vector_store import VectorStore

classifier = IntentClassifier()

if __name__ == "__main__":
    print("\n--- CHATBOT SẴN SÀNG ---")
    while True:
        query = input("User: ")
        print(classifier.predict(query))