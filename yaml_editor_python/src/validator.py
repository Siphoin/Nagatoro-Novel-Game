# src/validator.py
import os
from typing import Dict, Any, List

class StructureValidator:
    """
    Класс для проверки минимальной необходимой структуры языковой папки.
    Считается невалидной, если в корневой папке отсутствуют ключевые файлы.
    """

    # Определяем обязательные файлы, которые должны быть в корневой папке языка
    REQUIRED_ROOT_FILES: List[str] = [
        "metadata.yaml",
        "characters.yaml",
        "ui.yaml"
    ]

    def validate_structure(self, language_structure: Dict[str, Any]) -> bool:
        """
        Проверяет, содержит ли языковая структура обязательные файлы в корне.

        Args:
            language_structure: Словарь, полученный от LanguageService, 
                                содержащий 'root_path' и 'structure'.

        Returns:
            True, если структура валидна, False в противном случае.
        """
        root_path_normalized = language_structure.get('root_path')
        structure_map = language_structure.get('structure', {})
        
        if not root_path_normalized:
            return False

        # 1. Получаем список файлов, найденных в корневой папке
        root_files_found = structure_map.get(root_path_normalized, [])
        root_files_set = set(f.lower() for f in root_files_found)
        
        is_valid = True
        missing_files = []

        # 2. Проверяем наличие каждого обязательного файла
        for required_file in self.REQUIRED_ROOT_FILES:
            if required_file.lower() not in root_files_set:
                is_valid = False
                missing_files.append(required_file)

        if not is_valid:
            # Можно сохранить информацию об ошибке для отображения в UI
            self.last_error = f"Missing required files in root: {', '.join(missing_files)}"
            return False
        
        self.last_error = ""
        return True
    
    def get_last_error(self) -> str:
        """Возвращает последнее сообщение об ошибке валидации."""
        return getattr(self, 'last_error', "")