import joblib
import re

vectorizer = joblib.load("model/tfidf_vectorizer.pkl")
model = joblib.load("model/intent_model.pkl")

def preprocess(text):
    text = text.lower().strip()
    text = re.sub(r"[^\w\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễ"
                  r"ìíịỉĩòóọỏõôồốộổỗơờớợởỡ"
                  r"ùúụủũưừứựửữỳýỵỷỹđ]", "", text)
    text = re.sub(r"\s+", " ", text)
    return text

def predict(text, k=3):

    text = preprocess(text)

    vec = vectorizer.transform([text]) 

    probs = model.predict_proba(vec)[0]

    top_idx = probs.argsort()[::-1][:k]

    return [model.classes_[i] for i in top_idx]

def predict_debug(text, k=3):

    text = preprocess(text)

    vec = vectorizer.transform([text])

    probs = model.predict_proba(vec)[0]

    top_idx = probs.argsort()[::-1][:k]

    results = []
    for i in top_idx:
        results.append({
            "intent": model.classes_[i],
            "score": float(probs[i])
        })

    return results

