import csv

input_file = "data/data.txt"
output_file = "data/data.csv"

with open(input_file, "r", encoding="utf-8") as f_in, \
     open(output_file, "w", encoding="utf-8-sig", newline="") as f_out:

    writer = csv.writer(f_out)

    # header nếu cần
    writer.writerow(["text", "label"])

    for line in f_in:
        line = line.strip()
        if not line:
            continue

        # tách theo dấu phẩy đầu tiên
        parts = line.split(",", 1)

        if len(parts) == 2:
            text = parts[0].strip()
            label = parts[1].strip()

            writer.writerow([text, label])