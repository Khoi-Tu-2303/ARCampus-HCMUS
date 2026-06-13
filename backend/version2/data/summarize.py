import json 
import pandas as pd


with open("./version2/data/data.json", 'r', encoding='utf-8') as f:
    data = json.load(f)
    
intent  = [i["intent"] for i in data]
print(pd.DataFrame(intent).value_counts())