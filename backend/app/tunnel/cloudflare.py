import logging
import re
import subprocess
import threading
import time
from typing import Callable, Optional

logger = logging.getLogger(__name__)

_URL_PATTERN = re.compile(r"https://[a-zA-Z0-9\-]+\.trycloudflare\.com")
_URL_PATTERN_NAMED = re.compile(r"https://[a-zA-Z0-9\-\.]+\.trycloudflare\.com")


class CloudflareTunnel:
    """Manage the cloudflared subprocess lifecycle."""

    def __init__(
        self,
        port: int = 8000,
        on_url: Optional[Callable[[str], None]] = None,
        max_retries: int = 5,
        retry_delay: float = 3.0,
    ):
        self.port = port
        self.on_url = on_url
        self.max_retries = max_retries
        self.retry_delay = retry_delay

        self._process: Optional[subprocess.Popen] = None
        self._thread: Optional[threading.Thread] = None
        self._stop_event = threading.Event()
        self._current_url: Optional[str] = None

    @property
    def url(self) -> Optional[str]:
        return self._current_url

    def start(self) -> None:
        self._stop_event.clear()
        self._thread = threading.Thread(
            target=self._run_with_retry,
            name="cloudflare-tunnel",
            daemon=True,
        )
        self._thread.start()
        logger.info("Cloudflare tunnel thread started.")

    def stop(self) -> None:
        logger.info("Stopping Cloudflare tunnel...")
        self._stop_event.set()
        if self._process and self._process.poll() is None:
            self._process.terminate()
            try:
                self._process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self._process.kill()
        if self._thread:
            self._thread.join(timeout=10)
        logger.info("Cloudflare tunnel stopped.")

    def _run_with_retry(self) -> None:
        attempt = 0
        while not self._stop_event.is_set():
            if attempt >= self.max_retries:
                logger.error(
                    "Cloudflare tunnel failed after %d attempts.",
                    self.max_retries,
                )
                break

            logger.info("Starting cloudflared, attempt %d.", attempt + 1)
            try:
                self._launch_process()
            except FileNotFoundError:
                logger.error(
                    "`cloudflared` not found. Install it before starting the tunnel."
                )
                break
            except Exception:
                logger.exception("Unexpected error in cloudflared.")

            if not self._stop_event.is_set():
                attempt += 1
                logger.warning("Tunnel exited. Retrying in %.1fs.", self.retry_delay)
                time.sleep(self.retry_delay)

    def _launch_process(self) -> None:
        cmd = [
            "cloudflared",
            "tunnel",
            "--url",
            f"http://localhost:{self.port}",
            "--no-autoupdate",
        ]

        self._process = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,
        )

        for line in self._process.stdout:
            line = line.rstrip()
            if line:
                logger.debug("[cloudflared] %s", line)

            url = self._extract_url(line)
            if url and url != self._current_url:
                self._current_url = url
                logger.info("Cloudflare public URL: %s", url)
                if self.on_url:
                    threading.Thread(
                        target=self._safe_callback,
                        args=(url,),
                        daemon=True,
                    ).start()

            if self._stop_event.is_set():
                self._process.terminate()
                break

        self._process.wait()
        exit_code = self._process.returncode
        if exit_code not in (0, -15):
            logger.warning("cloudflared exited with code %d", exit_code)

    def _extract_url(self, line: str) -> Optional[str]:
        for pattern in (_URL_PATTERN, _URL_PATTERN_NAMED):
            match = pattern.search(line)
            if match:
                return match.group(0)
        return None

    def _safe_callback(self, url: str) -> None:
        try:
            self.on_url(url)
        except Exception:
            logger.exception("Error in on_url callback.")
