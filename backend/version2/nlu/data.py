"""
Dataset & label mappings cho Joint NLU (Intent + NER/slot filling)
"""

import json
import torch
from torch.utils.data import Dataset
from transformers import AutoTokenizer
from typing import Any


# Label maps

INTENT2ID = {
    "navigation": 0,
    "inform":     1,
    "unknown":    2,
    "general":    3,
}
ID2INTENT = {v: k for k, v in INTENT2ID.items()}
NUM_INTENTS = len(INTENT2ID)

ENTITIES = ["room", "building", "service", "department", "facility"]

SLOT_LABELS = ["O"]
for _ent in ENTITIES:
    SLOT_LABELS.append(f"B-{_ent}")
    SLOT_LABELS.append(f"I-{_ent}")

SLOT2ID  = {lbl: i for i, lbl in enumerate(SLOT_LABELS)}
ID2SLOT  = {i: lbl for lbl, i in SLOT2ID.items()}
NUM_SLOTS = len(SLOT_LABELS)

PAD_SLOT_ID = -100   # bỏ qua khi tính loss (special / padding tokens)


# Span to BIO alignment

def align_labels(
    entities: list[dict],
    offsets: list[tuple[int, int]],
) -> list[int]:
    """
    Chuyển danh sách span entities thành BIO label ids theo offset mapping.

    entities : [{"start": int, "end": int, "label": str}, ...]
    offsets  : [(char_start, char_end), ...] — từ tokenizer offset_mapping
    """
    labels = [PAD_SLOT_ID] * len(offsets)  # mặc định: ignore

    for i, (s, e) in enumerate(offsets):
        if s == e:
            continue       # special tokens ([CLS], [SEP], padding)
        labels[i] = SLOT2ID["O"]

    for ent in entities:
        ent_start, ent_end, ent_label = ent["start"], ent["end"], ent["label"]
        
        # clear ent_label
        if ent_label not in [f"{t}-{l}" for t in ("B","I") for l in ENTITIES]:
            ent_label_clean = ent_label.split("-")[-1] if "-" in ent_label else ent_label
        else:
            ent_label_clean = ent_label
    
        started = False
        for i, (s, e) in enumerate(offsets):
            if s == e:
                continue
            if e <= ent_start or s >= ent_end:
                continue
            if not started:
                labels[i] = SLOT2ID.get(f"B-{ent_label_clean}", SLOT2ID["O"])
                started = True
            else:
                labels[i] = SLOT2ID.get(f"I-{ent_label_clean}", SLOT2ID["O"])

    return labels


# Dataset

class CampusNLUDataset(Dataset):
    """
    Đọc file JSON với format:
    [
      {
        "text": "...",
        "intent": "navigation",           # hoặc list[str] nếu multi-label
        "entities": [
          {"start": 10, "end": 18, "label": "building"}
        ]
      },
      ...
    ]
    """

    def __init__(
        self,
        data_path: str,
        tokenizer: AutoTokenizer,
        max_length: int = 64,
    ):
        self.tokenizer  = tokenizer
        self.max_length = max_length

        with open(data_path, "r", encoding="utf-8") as f:
            self.samples = json.load(f)

    def __len__(self) -> int:
        return len(self.samples)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        sample = self.samples[idx]

        text     = sample["text"]
        entities = sample.get("entities", [])
        intent   = sample.get("intent", "unknown")

        # Tokenize input text.
        encoding = self.tokenizer(
            text,
            max_length=self.max_length,
            padding="max_length",
            truncation=True,
            return_offsets_mapping=True,
            return_tensors="pt",
        )

        input_ids      = encoding["input_ids"].squeeze(0)       # (L,)
        attention_mask = encoding["attention_mask"].squeeze(0)  # (L,)
        offsets        = encoding["offset_mapping"].squeeze(0).tolist()

        # Build labels.
        slot_labels = align_labels(entities, offsets)

        # single-label intent
        if isinstance(intent, list):
            intent_id = INTENT2ID.get(intent[0], INTENT2ID["unknown"])
        else:
            intent_id = INTENT2ID.get(intent, INTENT2ID["unknown"])

        return {
            "input_ids":      input_ids,
            "attention_mask": attention_mask,
            "intent_label":   torch.tensor(intent_id, dtype=torch.long),
            "slot_labels":    torch.tensor(slot_labels, dtype=torch.long),
        }
        
