# data_utils.py
import json
import random
from itertools import permutations
from torch.utils.data import Dataset
import torch
from transformers import AutoTokenizer

tokenizer = AutoTokenizer.from_pretrained("vinai/phobert-base")

def load_alias_pairs(path: str, strategy: str = "all") -> list[tuple[str, str]]:
    """
    Tạo cặp (anchor, positive) từ file JSON alias.
    
    strategy:
      "all"      → mọi tổ hợp 2 câu trong alias (C(n,2) cặp)
      "random"   → chỉ 1 cặp ngẫu nhiên mỗi alias (nhẹ hơn)
      "ordered"  → giữ thứ tự: (alias[0], alias[1]), (alias[0], alias[2])...
    """
    with open(path, encoding='utf-8') as f:
        data = json.load(f)
    
    pairs = []
    for item in data:
        aliases = item["alias"]
        if len(aliases) < 2:
            continue
        
        if strategy == "all":
            for a, b in permutations(aliases, 2):
                pairs.append((a, b))
        elif strategy == "random":
            a, b = random.sample(aliases, 2)
            pairs.append((a, b))
        elif strategy == "ordered":
            anchor = aliases[0]
            for positive in aliases[1:]:
                pairs.append((anchor, positive))
    
    return pairs


class EntityDataset(Dataset):
    """
    data: list[{"id": str, "alias": list[str]}]
    Mỗi alias là một sample, label là index của entity trong danh sách.
    """
    def __init__(self, data: list[dict], max_length: int = 128):
        self.samples = []   # (alias_text, label_idx)
        self.id2label = {}  # idx -> entity_id
        self.label2id = {}  # entity_id -> idx

        for idx, entity in enumerate(data):
            self.id2label[idx] = entity["id"]
            self.label2id[entity["id"]] = idx
            for alias in entity["alias"]:
                self.samples.append((alias, idx))

        self.max_length = max_length

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, i):
        text, label = self.samples[i]
        encoding = tokenizer(
            text,
            max_length=self.max_length,
            padding="max_length",
            truncation=True,
            return_tensors="pt",
        )
        return {
            "input_ids":      encoding["input_ids"].squeeze(0),
            "attention_mask": encoding["attention_mask"].squeeze(0),
            "label":          torch.tensor(label, dtype=torch.long),
        }
    
if __name__ == "__main__":
    pairs = load_alias_pairs("./version2/data/entities.json")
    print(pairs)