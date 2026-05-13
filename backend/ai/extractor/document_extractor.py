from typing import List, Dict
from ai.extractor.base_extractor import BaseExtractor


class DocumentServiceExtractor(BaseExtractor):
    """
    Extract entities cho document_service intent
    Output: list canonical document entities
    """

    DOCUMENT_MAP: Dict[str, Dict] = {
        "student_certificate": {
            "patterns": [
                "giấy xác nhận sinh viên",
                "xác nhận sinh viên",
                "giấy sinh viên",
                "giấy cnsv",
                "giấy chứng nhận sinh viên",
                "cnsv",
                "chứng nhận sinh viên",
                "tạm hoãn nghĩa vụ quân sự",
                "hoãn nvqs"
            ],
        },

        "academic_transcript": {
            "patterns": [
                "bảng điểm",
                "bảng điểm học tập",
                "bảng điểm giấy"
            ],
        },

        "activities_transcript": {
            "patterns": [
                "bảng điểm rèn luyện",
                "giấy rèn luyện",
                "điểm rèn luyện",
                "giấy chứng nhận điểm rèn luyện",
                "đrl",
                "drl"
            ],
        },

        "procedure_request": {
            "patterns": [
                "thủ tục",
                "quy trình",
                "xin giấy",
                "đăng ký giấy",
            ],
        }
    }

    def extract(self, query: str) -> List[str]:
        q = self.normalize(query)

        results: List[str] = []

        for name, config in self.DOCUMENT_MAP.items():
            for pattern in config["patterns"]:
                if pattern in q:
                    results.append(name)
                    break

        return results