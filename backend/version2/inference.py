"""
Inference — Joint NLU (Intent + NER)

Ví dụ:
    predictor = JointNLUPredictor("checkpoints/best_model.pt")
    result = predictor.predict("Phòng A101 ở tòa nhà B ở đâu?")
    print(result)
    # {
    #   "intent": "navigation",
    #   "intent_confidence": 0.97,
    #   "entities": [
    #     {"text": "A101", "label": "room",     "start": 6,  "end": 10},
    #     {"text": "B",    "label": "building",  "start": 19, "end": 20}
    #   ]
    # }
"""

import torch
import torch.nn.functional as F
from transformers import AutoTokenizer

from version2.nlu.model   import JointNLUModel
from version2.nlu.data import (
    NUM_INTENTS, NUM_SLOTS, SLOT2ID, ID2SLOT, ID2INTENT, PAD_SLOT_ID
)


class JointNLUPredictor:

    def __init__(
        self,
        checkpoint_path: str,
        model_name: str = "xlm-roberta-base",
        max_length: int = 64,
        device: str | None = None,
    ):
        self.device     = torch.device(device or ("cuda" if torch.cuda.is_available() else "cpu"))
        self.max_length = max_length
        self.tokenizer  = AutoTokenizer.from_pretrained(model_name)

        self.model = JointNLUModel(
            model_name=model_name,
            num_intents=NUM_INTENTS,
            num_slots=NUM_SLOTS,
        ).to(self.device)
        self.model.load_state_dict(
            torch.load(checkpoint_path, map_location=self.device)
        )
        self.model.eval()

    @torch.no_grad()
    def predict(self, text: str) -> dict:
        encoding = self.tokenizer(
            text,
            max_length=self.max_length,
            truncation=True,
            return_offsets_mapping=True,
            return_tensors="pt",
        )

        input_ids      = encoding["input_ids"].to(self.device)
        attention_mask = encoding["attention_mask"].to(self.device)
        offsets        = encoding["offset_mapping"].squeeze(0).tolist()

        intent_logits, slot_logits = self.model(input_ids, attention_mask)

        # ── Intent ────────────────────────────────────────────────────────
        intent_probs  = F.softmax(intent_logits, dim=-1).squeeze(0)
        print(intent_probs)
        intent_id     = intent_probs.argmax().item()
        intent_conf   = intent_probs[intent_id].item()
        intent_name   = ID2INTENT.get(intent_id, "unknown")

        # ── Slots → span entities ─────────────────────────────────────────
        slot_preds = slot_logits.argmax(dim=-1).squeeze(0).tolist()  # (L,)
        entities   = self._decode_bio(text, slot_preds, offsets)

        return {
            "intent":             intent_name,
            "intent_confidence":  round(intent_conf, 4),
            "entities":           entities,
        }

    def _decode_bio(
        self,
        text: str,
        slot_ids: list[int],
        offsets: list[tuple[int, int]],
    ) -> list[dict]:
        """Gom BIO labels thành span entities."""
        entities    = []
        current_ent = None

        for i, (s, e) in enumerate(offsets):
            if s == e:
                # special / padding token
                if current_ent is not None:
                    entities.append(current_ent)
                    current_ent = None
                continue

            label = ID2SLOT.get(slot_ids[i], "O")

            if label.startswith("B-"):
                if current_ent is not None:
                    entities.append(current_ent)
                current_ent = {
                    "text":  text[s:e],
                    "label": label[2:],
                    "start": s,
                    "end":   e,
                }

            elif label.startswith("I-") and current_ent is not None:
                # mở rộng span
                current_ent["end"]  = e
                current_ent["text"] = text[current_ent["start"]:e]

            else:
                if current_ent is not None:
                    entities.append(current_ent)
                    current_ent = None

        if current_ent is not None:
            entities.append(current_ent)

        return entities

# ── CLI nhanh ──────────────────────────────────────────────────────────────

if __name__ == "__main__":
    import sys
    ckpt = sys.argv[1] if len(sys.argv) > 1 else "checkpoints/model.pt"
    predictor = JointNLUPredictor(ckpt)

    while True:
        try:
            text = input("\nNhập câu: ").strip()
            if not text:
                continue
            result = predictor.predict(text)
            print(f"  Intent   : {result['intent']}  (conf={result['intent_confidence']})")
            print(f"  Entities : {result['entities']}")
        except KeyboardInterrupt:
            print("\nBye!")
            break