import os
import sys

# Các thư mục muốn bỏ qua
IGNORE_DIRS = {
    ".git",
    "__pycache__",
    "node_modules",
    ".venv",
    "venv",
    "dist",
    "build",
    ".idea",
    ".vscode"
}

# Các extension muốn đọc
VALID_EXTENSIONS = {
    ".py",
    ".js",
    ".ts",
    ".tsx",
    ".jsx",
    ".java",
    ".cpp",
    ".c",
    ".cs",
    ".go",
    ".rs",
    ".php",
    ".html",
    ".css",
    ".scss",
    ".json",
    ".xml",
    ".yaml",
    ".yml",
    ".md",
    ".sql",
    ".txt",
    ".sh"
}


def generate_tree(root_path):
    """
    Sinh cây thư mục dạng text
    """
    tree_lines = []

    def walk(dir_path, prefix=""):
        items = sorted(os.listdir(dir_path))

        # lọc thư mục ignore
        items = [
            item for item in items
            if item not in IGNORE_DIRS
        ]

        for index, item in enumerate(items):
            full_path = os.path.join(dir_path, item)
            is_last = index == len(items) - 1

            connector = "└── " if is_last else "├── "

            tree_lines.append(prefix + connector + item)

            if os.path.isdir(full_path):
                extension = "    " if is_last else "│   "
                walk(full_path, prefix + extension)

    tree_lines.append(os.path.basename(root_path))
    walk(root_path)

    return "\n".join(tree_lines)


def collect_code(root_path):
    """
    Gộp toàn bộ code thành text
    """
    collected = []

    for dirpath, dirnames, filenames in os.walk(root_path):

        # bỏ qua folder ignore
        dirnames[:] = [
            d for d in dirnames
            if d not in IGNORE_DIRS
        ]

        for filename in filenames:
            file_path = os.path.join(dirpath, filename)

            ext = os.path.splitext(filename)[1].lower()

            if ext not in VALID_EXTENSIONS:
                continue

            relative_path = os.path.relpath(file_path, root_path)

            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    content = f.read()

                collected.append("=" * 80)
                collected.append(f"FILE: {relative_path}")
                collected.append("=" * 80)
                collected.append(content)
                collected.append("\n\n")

            except Exception as e:
                collected.append("=" * 80)
                collected.append(f"FILE: {relative_path}")
                collected.append("=" * 80)
                collected.append(f"[ERROR READING FILE] {e}")
                collected.append("\n\n")

    return "\n".join(collected)


def main():
    if len(sys.argv) != 2:
        print("Usage:")
        print("python export_project.py <folder_path>")
        sys.exit(1)

    root_path = sys.argv[1]

    if not os.path.exists(root_path):
        print(f"Folder không tồn tại: {root_path}")
        sys.exit(1)

    root_path = os.path.abspath(root_path)

    # sinh cây thư mục
    tree = generate_tree(root_path)

    # gom code
    code_content = collect_code(root_path)

    # output file
    output_file = "project_dump.txt"

    with open(output_file, "w", encoding="utf-8") as f:
        f.write("PROJECT TREE\n")
        f.write("=" * 80)
        f.write("\n")
        f.write(tree)

        f.write("\n\n\n")
        f.write("PROJECT FILES\n")
        f.write("=" * 80)
        f.write("\n\n")

        f.write(code_content)

    print(f"✅ Đã export project vào: {output_file}")


if __name__ == "__main__":
    main()
    
# python code2text.py "C:\Users\WIN11\Downloads\Scripts\Scripts"