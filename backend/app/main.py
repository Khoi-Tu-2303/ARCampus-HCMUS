"""
AI Campus Chatbot — Backend Entry Point
=======================================
Khởi động FastAPI + Cloudflare Tunnel + Firebase publisher
bằng một lệnh duy nhất:

    python main.py
    hoặc
    uvicorn main:app --host 0.0.0.0 --port 8000
"""

import logging
import os
import sys
from contextlib import asynccontextmanager

import uvicorn
from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent
ENV_PATH = BASE_DIR / ".env"

load_dotenv(ENV_PATH)

from app.routes.auth import router as auth_router
from app.routes.conversations import router as conv_router
from app.tunnel import CloudflareTunnel, publish_url
from ai.intent.core.vector_store import VectorStore
from ai.intent.core.embedder import TextEmbedder

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger(__name__)


PORT = int(os.getenv("PORT", 8000))

def _on_tunnel_url(public_url: str) -> None:
    """
    Callback được gọi mỗi khi cloudflared publish một URL mới.
    Chạy trong background thread — KHÔNG được block quá lâu.
    """
    print(f"\n{'='*55}")
    print(f"  🌐  Backend running at: {public_url}")
    print(f"{'='*55}\n")

    try:
        if os.getenv("FIREBASE_ENABLED", "true").lower() == "true":
            publish_url(public_url)
            print("    Firebase updated successfully\n")
        else:
            logger.info("Firebase publishing disabled (FIREBASE_ENABLED=false).")
    except Exception as exc:
        logger.error("Failed to publish URL to Firebase: %s", exc)

tunnel = CloudflareTunnel(
    port=PORT,
    on_url=_on_tunnel_url,
    max_retries=10,
    retry_delay=5.0,
)

@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("Loading AI resources...")

    logger.info("AI resources loaded successfully.")

    logger.info("Starting Cloudflare Tunnel...")
    tunnel.start()
    yield
    logger.info("Shutting down Cloudflare Tunnel...")
    tunnel.stop()

app = FastAPI(
    title="AI Campus Chatbot API",
    version="1.0.0",
    lifespan=lifespan,
)

_allowed_origins = os.getenv("CORS_ORIGINS", "*").split(",")

app.add_middleware(
    CORSMiddleware,
    allow_origins=_allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(auth_router)
app.include_router(conv_router)


@app.get("/", tags=["Health"])
def health_check():
    return {
        "status": "ok",
        "tunnel_url": tunnel.url, 
    }


if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=PORT,
        reload=False, 
        log_level="info",
    )