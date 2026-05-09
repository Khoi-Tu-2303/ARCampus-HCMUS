from abc import ABC, abstractmethod
from typing import List


class BaseExtractor(ABC):
    """
    Base class cho tất cả Extractors.
    Mỗi extractor sẽ biến query string -> structured entity dict.
    """

    @abstractmethod
    def extract(self, query: str) -> List[str]:
        """Extract entities từ user query"""
        pass

    def normalize(self, text: str) -> str:
        """Chuẩn hóa input text"""
        return text.lower().strip()