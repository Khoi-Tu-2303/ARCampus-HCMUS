CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE,
    password_hash TEXT,
    full_name TEXT,
    student_id TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
