import json

def convert_entities(data):
    result = []

    for item in data:
        text = item["text"]
        new_entities = []

        for ent in item.get("entities", []):
            value = ent["value"]

            # tìm vị trí xuất hiện trong text
            start = text.find(value)

            if start == -1:
                # nếu không tìm thấy thì bỏ qua entity này
                continue

            end = start + len(value)

            new_entities.append({
                "label": ent["label"],
                "start": start,
                "end": end
            })

        result.append({
            "text": text.lower(),
            "intent": item["intent"],
            "entities": new_entities
        })

    return result


# ===== đọc file JSON =====
filenames = ["general.json", 
             "unknow.json",
            "inform_building.json",
            "inform_facility.json",
            "inform_multi_entity.json",
            "inform_room.json",
            "inform_no_entity.json",
            "navigation_building.json",
            "navigation_facility.json",
            "navigation_multi_entity.json",
            "navigation_no_entity.json",
            "navigation_room.json"]
output_file = "./version2/data/data.json"
data = []
for filename in filenames:
    input_file = f"./version2/data/input/{filename}"

    with open(input_file, "r", encoding="utf-8") as f:
        data.extend(convert_entities(json.load(f)))

with open(output_file, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print("Done converting!")