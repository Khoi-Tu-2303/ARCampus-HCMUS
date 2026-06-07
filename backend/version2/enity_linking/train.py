# train.py
import torch
from torch.utils.data import DataLoader
from torch.optim import AdamW
from transformers import get_linear_schedule_with_warmup
from tqdm import tqdm

from version2.enity_linking.data import load_alias_pairs, MNRLDataset
from model import BiEncoder
from loss import MultipleNegativesRankingLoss
import torch.nn.functional as F

def collate_fn(batch):
    anchors   = [pair[0] for pair in batch]
    positives = [pair[1] for pair in batch]
    return anchors, positives

@torch.no_grad()
def evaluate(
    model,
    dataloader,
    device="cuda",
    ks=(1, 5, 10),
):
    model.eval()

    all_anchor_embs = []
    all_positive_embs = []

    # Encode toàn bộ validation set
    for anchors, positives in tqdm(dataloader, desc="Evaluating"):

        emb_a, emb_p = model(
            anchors,
            positives,
            device
        )

        all_anchor_embs.append(emb_a.cpu())
        all_positive_embs.append(emb_p.cpu())

    A = torch.cat(all_anchor_embs, dim=0)
    P = torch.cat(all_positive_embs, dim=0)

    # Normalize để cosine similarity
    A = F.normalize(A, p=2, dim=1)
    P = F.normalize(P, p=2, dim=1)
    # Similarity matrix
    sim = A @ P.T
    N = sim.size(0)

    recalls = {k: 0 for k in ks}
    reciprocal_rank_sum = 0

    for i in range(N):

        scores = sim[i]

        ranking = torch.argsort(
            scores,
            descending=True
        )
        # vị trí positive đúng
        rank = (ranking == i).nonzero(
            as_tuple=True
        )[0].item() + 1
        reciprocal_rank_sum += 1.0 / rank
        for k in ks:
            if rank <= k:
                recalls[k] += 1

    results = {}
    for k in ks:
        results[f"Recall@{k}"] = recalls[k] / N

    results["MRR"] = reciprocal_rank_sum / N
    model.train()

    return results

def train(
    data_path: str = "./version2/data/entities.json",
    model_name: str = "intfloat/multilingual-e5-base",
    output_dir: str = "./version2/enity_linking",
    epochs: int = 3,
    batch_size: int = 64,   
    lr: float = 2e-5,
    warmup_ratio: float = 0.1,
    scale: float = 20.0,
    device: str = "cuda" if torch.cuda.is_available() else "cpu",
):
    # --- Data ---
    pairs = load_alias_pairs(data_path, strategy="all")
    dataset = MNRLDataset(pairs)
    loader = DataLoader(
        dataset, 
        batch_size=batch_size, 
        shuffle=True, 
        collate_fn=collate_fn,
        drop_last=True   # Giữ batch size đồng đều
    )
    valid_pairs = load_alias_pairs(
    "./version2/data/entities_val.json",
    strategy="all"
)
    valid_dataset = MNRLDataset(valid_pairs)

    valid_loader = DataLoader(
        valid_dataset,
        batch_size=64,
        shuffle=False,
        collate_fn=collate_fn
    )
    
    # --- Model & Loss ---
    model = BiEncoder(model_name).to(device)
    criterion = MultipleNegativesRankingLoss(scale=scale)
    
    # --- Optimizer ---
    optimizer = AdamW(model.parameters(), lr=lr, weight_decay=0.01)
    
    total_steps = len(loader) * epochs
    warmup_steps = int(total_steps * warmup_ratio)
    scheduler = get_linear_schedule_with_warmup(
        optimizer, 
        num_warmup_steps=warmup_steps,
        num_training_steps=total_steps
    )
    
    # --- Training ---
    model.train()
    for epoch in range(epochs):
        total_loss = 0
        for step, (anchors, positives) in enumerate(tqdm(loader, desc=f"Epoch {epoch+1}")):
            
            emb_a, emb_p = model(anchors, positives, device)
            loss = criterion(emb_a, emb_p)
            
            optimizer.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)  # Gradient clipping
            optimizer.step()
            scheduler.step()
            
            total_loss += loss.item()
            
            if step % 100 == 0:
                print(f"  Step {step} | Loss: {loss.item():.4f}")
        
        avg_loss = total_loss / len(loader)
        print(f"Epoch {epoch+1} — Avg Loss: {avg_loss:.4f}")
        metrics = evaluate(
            model,
            valid_loader,
            device
        )

        print(metrics)
    
    # --- Save ---
    model.encoder.save_pretrained(output_dir)
    model.tokenizer.save_pretrained(output_dir)
    print(f"Model saved to {output_dir}")
    return model
if __name__ == "__main__":
    print(torch.cuda.is_available())