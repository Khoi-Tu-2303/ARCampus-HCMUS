# loss.py
import torch
import torch.nn.functional as F

class MultipleNegativesRankingLoss(torch.nn.Module):
    """
    Với batch (a₁..aₙ, p₁..pₙ):
    - Tính similarity matrix S[i,j] = cos(aᵢ, pⱼ)  → shape (N, N)
    - Label: đường chéo chính (i==j) là positive
    - Loss = CrossEntropy(S * scale, labels=range(N))
    
    scale (temperature): giá trị lớn → margin cứng hơn (thường 20.0)
    """
    def __init__(self, scale: float = 20.0):
        super().__init__()
        self.scale = scale
        self.cross_entropy = torch.nn.CrossEntropyLoss()
    
    def forward(self, emb_anchor: torch.Tensor, emb_positive: torch.Tensor) -> torch.Tensor:
        # Cosine similarity matrix: (N, N)
        scores = torch.matmul(emb_anchor, emb_positive.T) * self.scale
        
        # Labels: mỗi anchor i → positive i (đường chéo)
        labels = torch.arange(scores.size(0), device=scores.device)
        
        # Symmetric loss (tùy chọn — cải thiện đáng kể)
        loss_a2p = self.cross_entropy(scores, labels)
        loss_p2a = self.cross_entropy(scores.T, labels)
        
        return (loss_a2p + loss_p2a) / 2