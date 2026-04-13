import pandas as pd
import re
from sklearn.model_selection import train_test_split
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import classification_report, accuracy_score
import joblib

def preprocess(text):
    text = text.lower().strip()
    text = re.sub(r"[^\w\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễ"
                  r"ìíịỉĩòóọỏõôồốộổỗơờớợởỡ"
                  r"ùúụủũưừứựửữỳýỵỷỹđ]", "", text)
    text = re.sub(r"\s+", " ", text)
    return text

df = pd.read_csv("data/data.csv")

df["text"] = df["text"].apply(preprocess)

X = df["text"]
y = df["label"]

vectorizer = TfidfVectorizer(
    ngram_range=(1,2),
    max_features=3000
)

X_vec = vectorizer.fit_transform(X)

model = LogisticRegression(max_iter=1000)
model.fit(X_vec, y)

joblib.dump(vectorizer, "model/tfidf_vectorizer.pkl")
joblib.dump(model, "model/intent_model.pkl")