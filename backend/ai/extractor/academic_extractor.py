from typing import List, Dict
from ai.extractor.base_extractor import BaseExtractor


class AcademicExtractor(BaseExtractor):
    """
    Extract entities cho academic_query intent
    Output: list canonical academic entities
    """

    ACADEMIC_MAP: Dict[str, Dict] = {
        "study_schedule": {
            "patterns": [
                "lịch học",
                "thời khóa biểu",
                "tkb"
            ],
        },

        "exam_schedule": {
            "patterns": [
                "lịch thi",
                "thi cử"
            ],
        },

        "course_registration": {
            "patterns": [
                "đăng ký học phần",
                "đăng ký môn"
            ],
        },

        "course_info": {
            "patterns": [
                "môn học",
                "thông tin môn học",
            ],
        },

        "study_time": {
            "patterns": [
                "thời gian học",
                "giờ học",
                "ca học",
                "tiết học",
                "tiết lý thuyết",
                "tiết thực hành"
            ],
        }
    }

    def extract(self, query: str) -> List[str]:
        q = self.normalize(query)

        results: List[str] = []

        for name, config in self.ACADEMIC_MAP.items():
            for pattern in config["patterns"]:
                if pattern in q:
                    results.append(name)
                    break

        return results