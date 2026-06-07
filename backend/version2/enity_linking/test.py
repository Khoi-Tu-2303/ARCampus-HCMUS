# hard_negative_mining.py
import torch
import torch.nn.functional as F
from itertools import combinations
import random
import json

def find_confusable_pairs(model, data: list[dict], device="cpu", threshold=0.75):
    """
    Tìm các cặp nhãn có embedding trung bình gần nhau (dễ nhầm).
    Trả về: list[(label_id_A, label_id_B, similarity_score)]
    """
    # Tính centroid embedding cho mỗi nhãn
    label_centroids = {}
    for item in data:
        with torch.no_grad():
            embs = model.encode(item["alias"], device)  # (20, D)
            centroid = embs.mean(dim=0)                  # (D,)
            label_centroids[item["id"]] = F.normalize(centroid, dim=0)
    
    # Tìm cặp gần nhau
    confusable = []
    ids = list(label_centroids.keys())
    for id_a, id_b in combinations(ids, 2):
        sim = torch.dot(label_centroids[id_a], label_centroids[id_b]).item()
        if sim > threshold:
            confusable.append((id_a, id_b, sim))
    
    return sorted(confusable, key=lambda x: -x[2])


class HardNegativeMNRLDataset(torch.utils.data.Dataset):
    """
    Dataset đảm bảo mỗi batch chứa các cặp confusable.
    """
    def __init__(self, data: list[dict], confusable_pairs: list[tuple], 
                 hard_neg_ratio: float = 0.3):
        """
        hard_neg_ratio: tỉ lệ anchor trong batch được ghép với hard negative
                        thay vì in-batch negative ngẫu nhiên
        """
        self.data = {item["id"]: item["alias"] for item in data}
        self.confusable_pairs = confusable_pairs  # [(id_a, id_b, sim), ...]
        self.hard_neg_ratio = hard_neg_ratio
        
        # Xây dựng lookup: nhãn → danh sách hard negatives của nó
        self.hard_negatives = {}
        for id_a, id_b, _ in confusable_pairs:
            self.hard_negatives.setdefault(id_a, []).append(id_b)
            self.hard_negatives.setdefault(id_b, []).append(id_a)
        
        # Tạo danh sách (anchor, positive, hard_negative_label_id)
        self.triplets = self._build_triplets()
    
    def _build_triplets(self):
        triplets = []
        for label_id, aliases in self.data.items():
            if label_id not in self.hard_negatives:
                continue
            for i, anchor in enumerate(aliases):
                for j, positive in enumerate(aliases):
                    if i == j:
                        continue
                    # Chọn 1 hard negative label ngẫu nhiên
                    neg_label_id = random.choice(self.hard_negatives[label_id])
                    neg_alias = random.choice(self.data[neg_label_id])
                    triplets.append((anchor, positive, neg_alias))
        return triplets
    
    def __len__(self):
        return len(self.triplets)
    
    def __getitem__(self, idx):
        return self.triplets[idx]
    
if __name__ == "__main__":
    with open("./version2/data/entities.json", encoding="utf-8") as f:
        entities = json.load(f)
    print(len(entities))

