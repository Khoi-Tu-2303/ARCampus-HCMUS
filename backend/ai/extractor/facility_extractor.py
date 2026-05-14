from typing import List, Dict
from ai.extractor.base_extractor import BaseExtractor


class FacilityExtractor(BaseExtractor):
    """
    Extract entities cho facility_query intent
    Output: list canonical entity names
    """

    FACILITY_MAP: Dict[str, Dict] = {
        "library": {
            "patterns": ["thư viện", "phòng đọc", "khu thư viện", "tv", "mượn sách", "trả sách"],
        },
        "canteen": {
            "patterns": ["căn tin", "nhà ăn", "canteen", "can tin", "cantin"],
        },
        "stadium": {
            "patterns": ["sân vận động", "stadium", "nhà thể dục", "gym", "chỗ học thể dục", "sân td", "svd", "sân bóng rổ", "sân đá banh", "sân pickleball", "sân cầu lông"],
        },
        "parking": {
            "patterns": ["nhà xe", "bãi xe", "gửi xe", "giữ xe", "để xe", "đậu xe"],
        },

        # buildings
        "building_a": {"patterns": ["tòa a", "toa a"]},
        "building_b": {"patterns": ["tòa b", "toa b"]},
        "building_c": {"patterns": ["tòa c", "toa c"]},
        "building_d": {"patterns": ["tòa d", "toa d"]},
        "building_e": {"patterns": ["tòa e", "toa e"]},
        "building_f": {"patterns": ["tòa f", "toa f"]},
        "building_g": {"patterns": ["tòa g", "toa g"]},
        "building_h": {"patterns": ["tòa h", "toa h"]},
        "building_ndh": {"patterns": ["nhà điều hành", "ndh", "nđh"]},

        # rooms
        "room_pdt": {"patterns": ["phòng pdt", "pdt", "phong dao tao", "phòng đào tạo", "p.dt"]},
        "room_pctsv": {"patterns": ["phòng ctsv", "ctsv", "phòng công tác sinh viên", "phòng công tác sv", "p.ctsv", "pctsv", "phong cong tac sinh vien"]},
        "room_medical": {"patterns": ["phòng y tế", "y tế", "kiểm tra sức khỏe"]},
        "room_resting": {"patterns": ["phòng nghỉ", "phòng nghỉ trưa", "ngủ trưa", "nghỉ trưa", "ngủ", "nghỉ" ]},
        "room_toilet": {"patterns": ["nhà vệ sinh", "toilet", "wc", "vs", "nvs", "nhà vs"]},
    }

    def extract(self, query: str) -> List[str]:
        q = self.normalize(query)

        results: List[str] = []

        for name, config in self.FACILITY_MAP.items():
            for pattern in config["patterns"]:
                if pattern in q:
                    results.append(name)
                    break 

        return results
