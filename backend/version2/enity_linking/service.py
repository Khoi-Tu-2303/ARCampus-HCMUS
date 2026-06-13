from typing import List, Optional
from dataclasses import dataclass
import logging
import torch
import torch.nn as nn
from transformers import AutoTokenizer, AutoModel
from rapidfuzz import fuzz
import re
from version2.schemas import MatchResult, Entity
import json

logger = logging.getLogger(__name__)

class PhoBERTClassifier(nn.Module):
    def __init__(self, num_labels: int, dropout: float = 0.1):
        super().__init__()
        self.bert = AutoModel.from_pretrained("vinai/phobert-base")
        self.dropout = nn.Dropout(dropout)
        self.classifier = nn.Linear(768, num_labels)

    def forward(self, input_ids, attention_mask):
        outputs = self.bert(input_ids=input_ids, attention_mask=attention_mask)
        cls = outputs.last_hidden_state[:, 0]
        cls = self.dropout(cls)
        return self.classifier(cls)


class EntityLinking:
    _instance = None  # singleton

    def __new__(cls, *args, **kwargs):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance

    def __init__(
        self,
        checkpoint_path="./version2/enity_linking/best_model.pt",
        threshold: float = 0.6,
        device: Optional[str] = None,
    ):
        # __init__ chạy lại mỗi lần gọi EntityLinking() dù là singleton
        # dùng flag để chỉ khởi tạo 1 lần
        if getattr(self, "_initialized", False):
            return

        self.pattern_room_anchor = re.compile(
            r'([A-Za-z]{1,8})([ \-.]?)(\d{3})',
            re.IGNORECASE
        )

        # Anchor B: [1 so][.][1 so] for nha dieu hanh.
        self.pattern_ndh_anchor = re.compile(
            r'(\d)[.](\d)'
        )
        
        self.ROOM_KEYWORDS = ["phòng", "phong"]
        self.NDH_KEYWORDS  = ["nhà điều hành", "nha dieu hanh", "ndh", "nđh"]
        
        self.threshold = threshold
        self.device    = torch.device(device or ("cuda" if torch.cuda.is_available() else "cpu"))

        # Load model resources.
        checkpoint = torch.load(checkpoint_path, map_location=self.device)
        self.num_labels = checkpoint["num_labels"]
        self.id2label = checkpoint["id2label"]
        with open("./version2/data/id.json", "r", encoding='utf-8') as file:
            self.ID = json.load(file)
        with open("./version2/data/alias.json", "r", encoding='utf-8') as file:
            self.ALIAS = json.load(file)
        self.tokenizer = AutoTokenizer.from_pretrained("vinai/phobert-base")
        self.model     = PhoBERTClassifier(num_labels=self.num_labels).to(self.device)
        self.model.load_state_dict(checkpoint["model_state"])
        self.model.eval()
        logger.info(
            "Loaded entity linking model from %s with F1=%.4f.",
            checkpoint_path,
            checkpoint["val_f1"],
        )

        self._initialized = True
        
    def _normalize_unicode(self, text: str) -> str:
        """Bỏ dấu tiếng Việt để so sánh dễ hơn."""
        import unicodedata
        text = unicodedata.normalize("NFD", text)
        text = "".join(c for c in text if unicodedata.category(c) != "Mn")
        return text.lower()
    
    def _best_fuzzy_score(self, candidate: str, keywords: list[str]) -> tuple[float, str]:
        """Trả về (score cao nhất, keyword khớp nhất).
        
        Nếu candidate rỗng thì hợp lệ luôn, trả (100.0, "").
        """
        candidate = self._normalize_unicode(candidate)
        if candidate == "":
            return (100.0, "")

        best_score = 0.0
        best_kw = ""
        for kw in keywords:
            if kw == "":
                continue
            score = fuzz.ratio(candidate, self._normalize_unicode(kw))
            if score > best_score:
                best_score, best_kw = score, kw

        return (best_score, best_kw)

    # Rule-based matching
    def _match_anchor_fuzzy(self, text: str, label: str) -> Optional[MatchResult]:
        if label != "room":
            return None
        
        text_stripped = text.strip()
        # Handle nha dieu hanh anchor.
        m = self.pattern_ndh_anchor.search(text_stripped)
        if m:
            # Use the text before the anchor as an optional keyword prefix.
            prefix_text = text_stripped[: m.start()].strip(" \t\n-.")
            score, _ = self._best_fuzzy_score(prefix_text, self.NDH_KEYWORDS)

            if score >= 70 or prefix_text == "":
                floor    = m.group(1)
                room_num = m.group(2)
                matched_id = f"NDH{floor}.{room_num}"
                matched_id = matched_id if matched_id in self.ID else None
                status = "matched" if matched_id in self.ID else "unknown"
                return MatchResult(
                    entity_text=text_stripped,
                    matched_id=f"NDH{floor}.{room_num}",
                    label=label,
                    score=score/100,
                    status=status,
                )

        # Handle regular room anchor.
        m = self.pattern_room_anchor.search(text_stripped)
        if m:
            prefix_text = text_stripped[: m.start()].strip(" \t\n-.")
            score, _ = self._best_fuzzy_score(prefix_text, self.ROOM_KEYWORDS)

            if score >= 70 or prefix_text == "":
                letter = m.group(1).upper()
                number = m.group(3)
                matched_id = f"{letter}{number}"
                matched_id = matched_id if matched_id in self.ID else None
                status = "matched" if matched_id in self.ID else "unknown"
                return MatchResult(
                    entity_text=text_stripped,
                    label=label,
                    matched_id=matched_id,
                    score=score/100,
                    status=status
                )

        return None

    # Alias search
    def _match_alias_search(self, text: str, label: str) -> Optional[MatchResult]:
        text_preprocessed = text.strip().lower()
        if text_preprocessed in self.ALIAS:
            return MatchResult(
                    entity_text=text_preprocessed,
                    label=label,
                    matched_id=self.ALIAS[text_preprocessed],
                    score=1,
                    status="matched"
                )
        return None

    # PhoBERT classifier
    def _match_phobert(self, text: str, label: str) -> MatchResult:
        encoding = self.tokenizer(
            text,
            max_length=128,
            padding="max_length",
            truncation=True,
            return_tensors="pt",
        )
        with torch.no_grad():
            logits = self.model(
                encoding["input_ids"].to(self.device),
                encoding["attention_mask"].to(self.device),
            )
            probs      = torch.softmax(logits, dim=-1).squeeze(0)
            top_prob, top_idx = probs.max(dim=-1)

        score = top_prob.item()

        if score < self.threshold:
            return MatchResult(entity_text=text, label=label, matched_id=None, score=score, status="unknown")

        return MatchResult(
            entity_text=text,
            label=label,
            matched_id=self.id2label[top_idx.item()],
            score=score,
            status="matched",
        )

    # Main pipeline
    def predict(self, entity : Entity) -> MatchResult:
        text = entity.text
        label = entity.label
        result = self._match_anchor_fuzzy(text, label)
        if result is not None:
            return result

        result = self._match_alias_search(text, label)
        if result is not None:
            return result
        return self._match_phobert(text, label)

    def predict_batch(self, entities: List[Entity]) -> List[MatchResult]:
        return [self.predict(entity) for entity in entities]
