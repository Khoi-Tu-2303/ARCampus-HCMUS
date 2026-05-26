from pathlib import Path
import sqlite3

BASE_DIR = Path(__file__).resolve().parents[1]
DB_PATH = BASE_DIR / 'campus.db'
SCHEMA_DIR = BASE_DIR / 'database' / 'schema'


def _apply_sql_file(cursor, path: Path):
    with open(path, 'r', encoding='utf-8') as f:
        cursor.executescript(f.read())


def init_db():
    conn = sqlite3.connect(DB_PATH)
    try:
        cursor = conn.cursor()
        cursor.execute('PRAGMA foreign_keys = ON;')

        files = [
            SCHEMA_DIR / 'auth' / 'users.sql',
            SCHEMA_DIR / 'auth' / 'roles.sql',
            SCHEMA_DIR / 'auth' / 'user_roles.sql',
            SCHEMA_DIR / 'ai' / 'conversations.sql',
            SCHEMA_DIR / 'ai' / 'messages.sql',
        ]

        for file in files:
            _apply_sql_file(cursor, file)

        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='messages';")
        if cursor.fetchone():
            cursor.execute("PRAGMA foreign_key_check;")

        conn.commit()
        print('Database initialized!')
    finally:
        conn.close()


if __name__ == '__main__':
    init_db()
