# src/models.py
import os
import collections
from typing import Any, Dict # <-- ИСПРАВЛЕНО: Импорт необходимых типов!

# -------------------------------------------------------------------
# 1. YamlTab (Модель вкладки)
# -------------------------------------------------------------------

class YamlTab:
    """
    Модель для вкладки редактора.
    Соответствует классу YamlTab из C# (с использованием Python-списков как стеков).
    """
    def __init__(self, file_path: str, yaml_text: str):
        self.file_path = file_path
        self._yaml_text = yaml_text
        self.is_dirty = False
        # Используем список как стек. В отличие от C# Stack<T>, 
        # здесь мы храним начальный текст в стеке
        self.undo_stack = [yaml_text] 
        self.redo_stack = []

    @property
    def yaml_text(self):
        return self._yaml_text

    @yaml_text.setter
    def yaml_text(self, new_text):
        """Простая логика для отметки 'dirty'."""
        if self._yaml_text != new_text:
            self.is_dirty = True
        self._yaml_text = new_text


# -------------------------------------------------------------------
# 2. LanguageService (Бизнес-логика сканирования)
# -------------------------------------------------------------------

class LanguageService:
    """
    Имитирует LanguageService.Editor, который сканирует файловую структуру
    и предоставляет данные для редактора.
    """
    
    def __init__(self):
        pass # Инициализация не нужна

    def get_language_structure_from_path(self, folder_path: str) -> Dict[str, Any]:
        """
        Реализация, имитирующая сканирование папки (как в C# AddFolderRecursive).
        Это метод, который требовался в src/view.py.
        
        Возвращает:
        {
            'root_path': str,
            'structure': {
                'folder/path': ['file1.yaml', 'file2.yaml'],
                'folder/path/subfolder': ['file3.yaml']
            }
        }
        """
        # Нормализуем путь
        normalized_path = os.path.normpath(folder_path)

        # Имитируем типовую структуру, которую должна найти функция сканирования
        structure = {
            # Корневая папка
            normalized_path: [
                "metadata.yaml", 
                "characters.yaml", 
                "terms.yaml", 
                "image.png",          
                "temp_file.yaml.meta" 
            ],
            # Подпапки
            os.path.normpath(os.path.join(normalized_path, "dialogues")): [
                "01_scene_a.yaml", 
                "02_scene_b.yaml"
            ],
            os.path.normpath(os.path.join(normalized_path, "tutorial")): [
                "tut_01_intro.yaml"
            ]
        }
        
        return {
            'root_path': normalized_path,
            'structure': structure
        }

    def get_language_structure(self, language_name: str) -> Dict[str, Any]:
        """Заглушка для совместимости."""
        return {'root_path': None, 'structure': {}}
    
    def get_language_path(self, language_name: str) -> str:
         """Заглушка для совместимости."""
         return os.path.join("temp_lang_data", language_name)


# -------------------------------------------------------------------
# 3. LanguageMetaData (Порт из LanguageMetaData.cs)
# -------------------------------------------------------------------
class LanguageMetaData:
    """
    Метаданные о языке локализации.
    """
    def __init__(self, name: str = "", author: str = "", version: int = 0):
        # NameLanguage (string)
        self.name_language: str = name
        
        # Author (string)
        self.author: str = author
        
        # Version (uint) -> int
        self.version: int = version


# -------------------------------------------------------------------
# 4. NodeLocalizationData (Порт из NodeLocalizationData.cs)
# -------------------------------------------------------------------
class NodeLocalizationData:
    """
    Данные локализации для отдельного узла (диалоговой ветки, и т.п.).
    """
    def __init__(self, guid: str = "", value: Any = None):
        # GUID (string)
        self.guid: str = guid
        
        # Value (object) -> Any
        self.value: Any = value


# -------------------------------------------------------------------
# 5. CharacterLocalizationData (Порт из CharacterLocalizationData.cs)
# -------------------------------------------------------------------
class CharacterLocalizationData:
    """
    Данные локализации для персонажа.
    """
    def __init__(self, guid: str = "", name: str = "", description: str = ""):
        # GUID (string)
        self.guid: str = guid
        
        # Name (string)
        self.name: str = name
        
        # Description (string)
        self.description: str = description