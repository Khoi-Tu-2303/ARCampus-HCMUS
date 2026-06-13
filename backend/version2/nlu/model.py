"""
Joint NLU Model — Intent Classification + NER (BIO tagging)
Backbone: xlm-roberta-base
"""

import torch
import torch.nn as nn
from transformers import AutoModel


class JointNLUModel(nn.Module):
    """
    Shared XLM-RoBERTa encoder với hai đầu ra:
      - intent_logits : (B, num_intents)      — phân loại ý định
      - slot_logits   : (B, seq_len, num_slots) — gán nhãn BIO từng token
    """

    def __init__(
        self,
        model_name: str,
        num_intents: int,
        num_slots: int,
        dropout: float = 0.1,
    ):
        super().__init__()
        self.encoder = AutoModel.from_pretrained(model_name)
        hidden = self.encoder.config.hidden_size  # 768 với base

        self.dropout = nn.Dropout(dropout)

        # Intent head: dùng [CLS] token
        self.intent_head = nn.Sequential(
            nn.Linear(hidden, hidden // 2),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(hidden // 2, num_intents),
        )

        # Slot/NER head: dùng toàn bộ sequence
        self.slot_head = nn.Linear(hidden, num_slots)

    def forward(
        self,
        input_ids: torch.Tensor,          # (B, L)
        attention_mask: torch.Tensor,      # (B, L)
    ):
        outputs = self.encoder(
            input_ids=input_ids,
            attention_mask=attention_mask,
        )

        sequence_output = self.dropout(outputs.last_hidden_state)  # (B, L, H)
        cls_output = sequence_output[:, 0, :]                       # (B, H)

        intent_logits = self.intent_head(cls_output)                # (B, I)
        slot_logits   = self.slot_head(sequence_output)             # (B, L, S)

        return intent_logits, slot_logits