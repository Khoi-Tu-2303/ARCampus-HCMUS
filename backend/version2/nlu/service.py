import torch
import torch.nn.functional as F
import pandas as pd
import json
from transformers import AutoTokenizer
from version2.schemas import NLUResult, Intent, IntentType, Entity
from version2.nlu.model import JointNLUModel
from version2.nlu.data import (
    NUM_INTENTS,
    NUM_SLOTS,
    ID2SLOT,
    ID2INTENT,
)

class IntentEnityClassifier:
    _instance = None

    def __new__(
        cls,
        checkpoint_path="checkpoints/model.pt",
        model_name="xlm-roberta-base",
        max_length=64,
        device=None,
    ):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance

    def __init__(
        self,
        checkpoint_path="checkpoints/model.pt",
        model_name="xlm-roberta-base",
        max_length=64,
        device=None,
    ):
        if self._initialized:
            return

        self.device = torch.device(
            device or ("cuda" if torch.cuda.is_available() else "cpu")
        )

        self.max_length = max_length

        print("Loading tokenizer...")
        self.tokenizer = AutoTokenizer.from_pretrained(model_name)

        print("Loading model...")
        self.model = JointNLUModel(
            model_name=model_name,
            num_intents=NUM_INTENTS,
            num_slots=NUM_SLOTS,
        ).to(self.device)

        self.model.load_state_dict(
            torch.load(checkpoint_path, map_location=self.device)
        )

        self.model.eval()

        self._initialized = True

    @torch.no_grad()
    def predict(self, text):
        encoding = self.tokenizer(
            text,
            max_length=self.max_length,
            truncation=True,
            return_offsets_mapping=True,
            return_tensors="pt",
        )

        input_ids = encoding["input_ids"].to(self.device)
        attention_mask = encoding["attention_mask"].to(self.device)
        offsets = encoding["offset_mapping"].squeeze(0).tolist()

        intent_logits, slot_logits = self.model(
            input_ids,
            attention_mask
        )

        intent_probs = torch.sigmoid(intent_logits).squeeze(0)

        best_idx = torch.argmax(intent_probs).item()
        best_prob = intent_probs[best_idx].item()

        intents = [Intent(IntentType(ID2INTENT[best_idx]), best_prob)]

        entities = self._decode_bio(
            text,
            slot_logits.argmax(dim=-1).squeeze(0).tolist(),
            offsets,
        )
        
        entities = [Entity(e.get("text"), e.get("label")) for e in entities] 

        return NLUResult(intents, entities, text)

    def _decode_bio(self, text, slot_ids, offsets):
        entities = []
        current = None

        for i, (s, e) in enumerate(offsets):
            if s == e:
                if current:
                    entities.append(current)
                    current = None
                continue

            label = ID2SLOT.get(slot_ids[i], "O")

            if label.startswith("B-"):
                if current:
                    entities.append(current)

                current = {
                    "text": text[s:e],
                    "label": label[2:],
                    "start": s,
                    "end": e,
                }

            elif label.startswith("I-") and current:
                current["end"] = e
                current["text"] = text[current["start"]:e]

            else:
                if current:
                    entities.append(current)
                    current = None

        if current:
            entities.append(current)

        return entities

if __name__ == "__main__":
    predictor = IntentEnityClassifier()
    while True:
        q = input("q = ")
        print(predictor.predict(q))
