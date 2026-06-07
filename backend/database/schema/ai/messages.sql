-- CREATE TABLE IF NOT EXISTS messages (
--     id INTEGER PRIMARY KEY AUTOINCREMENT,
--     conversation_id TEXT,
--     role TEXT NOT NULL CHECK(role IN ('user', 'assistant')),
--     content TEXT NOT NULL,
--     metadata TEXT DEFAULT '{}',
--     created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
--     FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
-- );

-- CREATE INDEX IF NOT EXISTS idx_messages_session ON messages(conversation_id, created_at);

CREATE TABLE IF NOT EXISTS messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    conversation_id TEXT NOT NULL,
    role TEXT NOT NULL CHECK(role IN ('user', 'assistant')),
    content TEXT NOT NULL,

    -- NLU data (JSON arrays, nullable khi chưa phân tích)
    intents TEXT DEFAULT NULL,      -- JSON: [{type, confidence}, ...]
    entities TEXT DEFAULT NULL,     -- JSON: [{text, label, matched_id, score, status, ...}, ...]

    -- Flag tính toán nhanh (cập nhật cùng lúc với intents/entities)
    has_intents BOOLEAN GENERATED ALWAYS AS (
        intents IS NOT NULL AND intents != '[]' AND intents != 'null'
    ) VIRTUAL,
    has_entities BOOLEAN GENERATED ALWAYS AS (
        entities IS NOT NULL AND entities != '[]' AND entities != 'null'
    ) VIRTUAL,

    metadata TEXT DEFAULT '{}',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
);

-- Index chính: tìm kiếm theo conversation + thứ tự thời gian
CREATE INDEX IF NOT EXISTS idx_messages_conv_time
    ON messages(conversation_id, created_at DESC);

-- Index hỗ trợ lọc các message có NLU data
CREATE INDEX IF NOT EXISTS idx_messages_nlu
    ON messages(conversation_id, has_intents, has_entities, created_at DESC);