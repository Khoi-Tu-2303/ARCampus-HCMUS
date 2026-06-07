import json
import random
from pathlib import Path

# ======================
# CONFIG
# ======================
input_file = "./version2/data/data.json"   # đổi theo file của bạn
output_dir = "./version2/data/"

train_ratio = 0.8
val_ratio = 0.1
test_ratio = 0.1

seed = 42
random.seed(seed)

# ======================
# LOAD DATA
# ======================
with open(input_file, "r", encoding="utf-8") as f:
    data = json.load(f)

if not isinstance(data, list):
    raise ValueError("JSON phải là list[dict]")

random.shuffle(data)

n = len(data)
train_end = int(n * train_ratio)
val_end = train_end + int(n * val_ratio)

train_data = data[:train_end]
val_data = data[train_end:val_end]
test_data = data[val_end:]

# ======================
# SAVE FILES
# ======================
with open(output_dir + "train.json", "w", encoding="utf-8") as f:
    json.dump(train_data, f, ensure_ascii=False, indent=2)

with open(output_dir + "val.json", "w", encoding="utf-8") as f:
    json.dump(val_data, f, ensure_ascii=False, indent=2)

with open(output_dir + "test.json", "w", encoding="utf-8") as f:
    json.dump(test_data, f, ensure_ascii=False, indent=2)

print(f"Done!")
print(f"Total: {n}")
print(f"Train: {len(train_data)}")
print(f"Val:   {len(val_data)}")
print(f"Test:  {len(test_data)}")