"""
Training pipeline — Joint NLU (Intent + NER)
"""

import os
import json
import torch
import torch.nn as nn
from torch.utils.data import DataLoader
from transformers import AutoTokenizer, get_linear_schedule_with_warmup
from torch.optim import AdamW
from tqdm import tqdm
from sklearn.metrics import precision_recall_fscore_support, accuracy_score

from version2.nlu.model   import JointNLUModel
from version2.nlu.data import CampusNLUDataset, NUM_INTENTS, NUM_SLOTS, PAD_SLOT_ID


# ── Config ───────────────────────────────────────────────────────────────────

CFG = {
    "model_name":   "xlm-roberta-base",
    "max_length":   64,
    "batch_size":   16,
    "epochs":       10,
    "lr":           2e-5,
    "weight_decay": 0.01,
    "warmup_ratio": 0.1,
    "intent_weight": 1.0,   # lambda cho intent loss
    "slot_weight":   1.0,   # lambda cho slot loss
    "dropout":       0.1,
    "save_dir":      "checkpoints",
    "train_path":    "./version2/data/train.json",
    "dev_path":      "./version2/data/val.json",
}


# ── Loss ─────────────────────────────────────────────────────────────────────

def joint_loss(
    intent_logits: torch.Tensor,   # (B, I)
    slot_logits:   torch.Tensor,   # (B, L, S)
    intent_labels: torch.Tensor,   # (B,)
    slot_labels:   torch.Tensor,   # (B, L)
    intent_w: float = 1.0,
    slot_w:   float = 1.0,
) -> tuple[torch.Tensor, torch.Tensor, torch.Tensor]:

    intent_loss = nn.CrossEntropyLoss()(intent_logits, intent_labels)

    # slot: flatten (B, L, S) to (B*L, S) and skip PAD_SLOT_ID (-100)
    B, L, S = slot_logits.shape
    slot_loss = nn.CrossEntropyLoss(ignore_index=PAD_SLOT_ID)(
        slot_logits.view(B * L, S),
        slot_labels.view(B * L),
    )

    total = intent_w * intent_loss + slot_w * slot_loss
    return total, intent_loss, slot_loss


# ── Metric helpers ────────────────────────────────────────────────────────────

@torch.no_grad()
def evaluate(model, loader, device) -> dict:
    model.eval()

    all_intent_preds  = []
    all_intent_labels = []
    all_slot_preds    = []
    all_slot_labels   = []

    total_loss        = 0.0
    total_intent_loss = 0.0
    total_slot_loss   = 0.0
    num_batches       = 0

    for batch in loader:
        input_ids      = batch["input_ids"].to(device)
        attention_mask = batch["attention_mask"].to(device)
        intent_labels  = batch["intent_label"].to(device)
        slot_labels    = batch["slot_labels"].to(device)

        intent_logits, slot_logits = model(input_ids, attention_mask)

        # loss
        loss, i_loss, s_loss = joint_loss(
            intent_logits, slot_logits, intent_labels, slot_labels
        )
        total_loss        += loss.item()
        total_intent_loss += i_loss.item()
        total_slot_loss   += s_loss.item()
        num_batches       += 1

        # intent
        intent_pred = intent_logits.argmax(dim=-1)
        all_intent_preds.extend(intent_pred.cpu().tolist())
        all_intent_labels.extend(intent_labels.cpu().tolist())

        # slot (flatten, ignore PAD_SLOT_ID)
        slot_pred = slot_logits.argmax(dim=-1)
        mask = slot_labels != PAD_SLOT_ID
        all_slot_preds.extend(slot_pred[mask].cpu().tolist())
        all_slot_labels.extend(slot_labels[mask].cpu().tolist())

    # intent metrics
    intent_p, intent_r, intent_f1, _ = precision_recall_fscore_support(
        all_intent_labels, all_intent_preds, average="weighted", zero_division=0
    )

    # slot metrics
    slot_p, slot_r, slot_f1, _ = precision_recall_fscore_support(
        all_slot_labels, all_slot_preds, average="weighted", zero_division=0
    )

    n = max(num_batches, 1)
    return {
        # loss
        "loss":               total_loss        / n,
        "intent_loss":        total_intent_loss / n,
        "slot_loss":          total_slot_loss   / n,
        # intent metrics
        "intent_acc":         accuracy_score(all_intent_labels, all_intent_preds),
        "intent_precision":   intent_p,
        "intent_recall":      intent_r,
        "intent_f1":          intent_f1,
        # slot metrics
        "slot_acc":           accuracy_score(all_slot_labels, all_slot_preds),
        "slot_precision":     slot_p,
        "slot_recall":        slot_r,
        "slot_f1":            slot_f1,
    }

# ── Train ─────────────────────────────────────────────────────────────────────

def train():
    device    = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    tokenizer = AutoTokenizer.from_pretrained(CFG["model_name"])

    train_ds = CampusNLUDataset(CFG["train_path"], tokenizer, CFG["max_length"])
    dev_ds   = CampusNLUDataset(CFG["dev_path"],   tokenizer, CFG["max_length"])

    train_loader = DataLoader(train_ds, batch_size=CFG["batch_size"], shuffle=True)
    dev_loader   = DataLoader(dev_ds,   batch_size=CFG["batch_size"])

    model = JointNLUModel(
        model_name=CFG["model_name"],
        num_intents=NUM_INTENTS,
        num_slots=NUM_SLOTS,
        dropout=CFG["dropout"],
    ).to(device)

    # ── Optimizer ─────────────────────────────────────────────────────────
    
    # Chia các tham số thành 2 nhóm: decay, no_decay
    # Weight Decay là một kỹ thuật regularization dùng để giảm overfitting 
    # bằng cách phạt các trọng số (weights) có giá trị quá lớn.
    no_decay = ["bias", "LayerNorm.weight"]
    param_groups = [
        {
            "params": [p for n, p in model.named_parameters()
                       if not any(nd in n for nd in no_decay)],
            "weight_decay": CFG["weight_decay"],
        },
        {
            "params": [p for n, p in model.named_parameters()
                       if any(nd in n for nd in no_decay)],
            "weight_decay": 0.0,
        },
    ]
    optimizer = AdamW(param_groups, lr=CFG["lr"])

    total_steps  = len(train_loader) * CFG["epochs"]
    warmup_steps = int(total_steps * CFG["warmup_ratio"])
    scheduler    = get_linear_schedule_with_warmup(
        optimizer, warmup_steps, total_steps
    ) # điều chỉnh lr dựa vào warmup_steps

    os.makedirs(CFG["save_dir"], exist_ok=True)
    history = []
    for epoch in range(1, CFG["epochs"] + 1):
        model.train()
        total_loss = intent_loss_sum = slot_loss_sum = 0.0

        # tqdm là thư viện hiển thị thanh tiến trình
        for batch in tqdm(train_loader, desc=f"Epoch {epoch}/{CFG['epochs']}"):
            input_ids      = batch["input_ids"].to(device)
            attention_mask = batch["attention_mask"].to(device)
            intent_labels  = batch["intent_label"].to(device)
            slot_labels    = batch["slot_labels"].to(device)

            intent_logits, slot_logits = model(input_ids, attention_mask)

            loss, i_loss, s_loss = joint_loss(
                intent_logits, slot_logits,
                intent_labels, slot_labels,
                CFG["intent_weight"], CFG["slot_weight"],
            )
            
            # Xóa gradient cũ của tất cả tham số.
            # PyTorch mặc định cộng dồn gradient sau mỗi lần backward(),
            # nên cần reset trước khi tính gradient cho batch mới.
            optimizer.zero_grad()
            
            # Backpropagation:
            # Tính gradient của loss đối với toàn bộ tham số trong model.
            # Kết quả được lưu vào param.grad.
            loss.backward()
            
            # Gradient Clipping:
            # Giới hạn norm của gradient tối đa bằng 1.0 để tránh
            # hiện tượng Gradient Explosion khi huấn luyện Transformer.
            nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            
            # Cập nhật trọng số của model bằng AdamW.
            # Sử dụng gradient vừa tính để điều chỉnh các weight.
            optimizer.step()
            
            # Cập nhật learning rate theo scheduler.
            scheduler.step()

            # Tính tổng loss của epoch
            total_loss     += loss.item()
            intent_loss_sum += i_loss.item()
            slot_loss_sum   += s_loss.item()

        n = len(train_loader)
        train_metrics = {
            "loss":        total_loss      / n,
            "intent_loss": intent_loss_sum / n,
            "slot_loss":   slot_loss_sum   / n, 
        }
        dev_metrics = evaluate(model, dev_loader, device)
        print(
            f"\nEpoch {epoch}/{CFG['epochs']}\n"
            f"  Train: loss={train_metrics['loss']:.4f}  intent_loss={train_metrics['intent_loss']:.4f}  slot_loss={train_metrics['slot_loss']:.4f}\n"
            f"  Dev:   loss={dev_metrics['loss']:.4f}  intent_f1={dev_metrics['intent_f1']:.4f}  slot_f1={dev_metrics['slot_f1']:.4f}"
        )
        epoch_log = {
            "epoch": epoch,
            "train": train_metrics,
            "dev":   dev_metrics,
        }
        
        history.append(epoch_log)
        log_path = os.path.join(CFG["save_dir"], "history.json")
        with open(log_path, "w", encoding="utf-8") as f:
            json.dump(history, f, indent=2, ensure_ascii=False)

        save_path = os.path.join(CFG["save_dir"], f"model_epoch{epoch}.pt")
        torch.save(model.state_dict(), save_path)
        print(f"          Saved model to {save_path}")


if __name__ == "__main__":
    train()
    pass
