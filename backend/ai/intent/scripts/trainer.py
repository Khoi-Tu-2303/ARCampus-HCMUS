import os
import csv
import numpy as np
from dotenv import load_dotenv
from huggingface_hub import login
from ai.intent.core.config import IntentConfig
from ai.intent.core.embedder import TextEmbedder

class IntentTrainer:
    def __init__(self):
        self.config = IntentConfig()
        self.embedder = TextEmbedder()
        self._setup_huggingface()

    def _setup_huggingface(self):
        load_dotenv()
        hf_token = os.getenv("HF_TOKEN")
        if hf_token:
            print("[DEBUG] Đã tìm thấy HF_TOKEN. Đang tiến hành login HuggingFace...")
            login(hf_token)
            print("[DEBUG] Login HuggingFace thành công!")
        else:
            print("[WARNING] Không tìm thấy HF_TOKEN trong biến môi trường. Bỏ qua bước login.")

    def txt_to_csv(self):
        """Chuyển đổi data.txt sang data.csv an toàn"""
        print("Converting TXT to CSV...")
        with open(self.config.TXT_PATH, "r", encoding="utf-8") as f_in, \
             open(self.config.CSV_PATH, "w", encoding="utf-8-sig", newline="") as f_out:
            
            writer = csv.writer(f_out)
            writer.writerow(["text", "label"])

            for line in f_in:
                line = line.strip()
                if not line:
                    continue
                parts = line.rsplit(",", 1)
                if len(parts) == 2:
                    writer.writerow([parts[0].strip(), parts[1].strip()])

    def train_and_save(self):
        """Đọc CSV, tạo Embeddings và lưu ra NPZ"""
        print("Starting embedding process...")
        texts, intents, targets, metadata = [], [], [], []
        
        with open(self.config.CSV_PATH, "r", encoding="utf-8-sig") as f:
            reader = csv.DictReader(f)
            for row in reader:
                texts.append(row["text"])
                intents.append(row["intent"])
                targets.append(row["target"])
                metadata.append(row["metadata"])

        # Encode toàn bộ (batch)
        embeddings = self.embedder.encode_batch(texts)

        # Đảm bảo thư mục tồn tại
        os.makedirs(self.config.DATA_DIR, exist_ok=True)

        np.savez(
            self.config.VECTOR_PATH,
            texts=texts,
            intents=intents,
            vectors=embeddings,
            targets=targets,
            metadata=metadata
        )
        print(f"Successfully trained! Saved {len(texts)} samples to {self.config.VECTOR_PATH}.")
        print(f"Embedding shape: {embeddings.shape}")

    def run_pipeline(self):
        # self.txt_to_csv()
        self.train_and_save()

if __name__ == "__main__":
    trainer = IntentTrainer()
    trainer.run_pipeline()