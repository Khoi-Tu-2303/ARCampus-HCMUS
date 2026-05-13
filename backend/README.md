python -m venv venv //Tạo môi trường
venv\Script\activate  //Vào môi trường
pip install -r requirements.txt //Tải các thư viện về

// Khi tải thư viện mới
pip freeze > requirements.txt //trước khi push lên git


# AI Campus Chatbot Backend (FastAPI + SQLite)

## Run
```bash
cd campus_backend
python -m venv venv
# Windows
.\venv\Scripts\activate
# macOS/Linux
source .venv/bin/activate
pip install -r requirements.txt
python -m database.init_db
uvicorn app.main:app --reload
```

## Main API
- `POST /api/register`
- `POST /api/login`
- `POST /api/guest`
- `DELETE /api/guest/{guest_id}`
- `POST /api/conversations`
- `GET /api/conversations?id=...`
- `GET /api/conversations/{conversation_id}/messages`
- `POST /api/conversations/{conversation_id}/messages`
- `PATCH /api/conversations/{conversation_id}`
- `DELETE /api/conversations/{conversation_id}`