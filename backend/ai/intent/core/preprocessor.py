import re

class TextPreprocessor:
    """Class chịu trách nhiệm làm sạch dữ liệu, dùng chung cho cả lúc Train và Predict"""
    
    @staticmethod
    def clean(text: str) -> str:
        text = text.lower().strip()
        # Loại bỏ dấu câu, giữ lại chữ cái tiếng Việt và khoảng trắng
        text = re.sub(
            r"[^\w\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]", 
            "", 
            text
        )
        # Bỏ khoảng trắng thừa
        text = re.sub(r"\s+", " ", text)
        return text