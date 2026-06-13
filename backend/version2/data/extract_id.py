import json

with open("./version2/data/entities.json", "r", encoding='utf-8') as file:
    data = json.load(file)

labels = [d['id'] for d in data]
print(labels)