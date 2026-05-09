from ai.intent.core.classifier import IntentClassifier

classifier = IntentClassifier()

if __name__ == "__main__":
    while True:
        query = input("User: ")
        print(classifier.predict(query))