# model.py
import torch
import torch.nn as nn
import torch.nn.functional as F
from transformers import AutoModel

class PhoBERTClassifier(nn.Module):
    def __init__(self, num_labels: int, dropout: float = 0.1):
        super().__init__()
        self.bert = AutoModel.from_pretrained("vinai/phobert-base")
        self.dropout = nn.Dropout(dropout)
        self.classifier = nn.Linear(768, num_labels)

    def forward(self, input_ids, attention_mask):
        outputs = self.bert(input_ids=input_ids, attention_mask=attention_mask)
        cls = outputs.last_hidden_state[:, 0]   # CLS token
        cls = self.dropout(cls)                  # thêm dropout tránh overfit
        return self.classifier(cls)