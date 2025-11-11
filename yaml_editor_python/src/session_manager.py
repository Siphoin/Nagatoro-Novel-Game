# src/session_manager.py
import os
import json
import sys  
from typing import Dict, Any, List, TYPE_CHECKING
from PyQt5.QtGui import QColor 

from models import YamlTab 

# Для статической проверки типов без циклического импорта в рантайме
if TYPE_CHECKING:
    from view import YAMLEditorWindow
    from PyQt5.QtWidgets import QTextEdit 

class SessionManager:
    """
    Управляет сохранением и загрузкой состояния сессии редактора.
    Включает сохранение последнего открытого пути для QFileDialog.
    """
    
    SESSION_FILENAME = "session.json"

    def __init__(self, parent_window: 'YAMLEditorWindow'):
        self.parent_window = parent_window
        
        if getattr(sys, 'frozen', False):
            base_path = os.path.dirname(sys.executable)
        else:
            try:
                base_path = os.path.dirname(os.path.abspath(__file__))
            except NameError:
                 base_path = os.getcwd()
            
        self.session_file_path = os.path.join(base_path, self.SESSION_FILENAME)
        

    def save_session(self):
        """
        Сохраняет текущее состояние приложения в session.json.
        """
        current_tab_index = -1
        if self.parent_window.current_tab:
            try:
                current_tab_index = self.parent_window.open_tabs.index(self.parent_window.current_tab)
            except ValueError:
                 current_tab_index = -1
        
        # Если проект не открыт, сохраняем только путь для диалога и выходим
        if not self.parent_window.root_lang_path_normalized:
            if os.path.exists(self.session_file_path):
                 try:
                    os.remove(self.session_file_path) 
                 except Exception as e:
                    print(f"Warning: Could not clear session file: {e}")
            
            if self.parent_window._last_open_dir:
                session_data = {'last_open_dir': self.parent_window._last_open_dir}
            else:
                return # Нечего сохранять
        else:
            session_data = {
                'root_path': self.parent_window.root_lang_path_normalized,
                'current_tab_index': current_tab_index,
                'open_tabs': [],
                'foldouts': self.parent_window._foldouts,
                'last_open_dir': self.parent_window._last_open_dir 
            }

            # Сохраняем только путь и is_dirty (и dirty_content)
            for tab in self.parent_window.open_tabs:
                if tab.file_path.lower().startswith(self.parent_window.root_lang_path_normalized):
                     tab_data = {
                        'file_path': tab.file_path,
                        'is_dirty': tab.is_dirty,
                        'dirty_content': tab.yaml_text if tab.is_dirty else None 
                    }
                     session_data['open_tabs'].append(tab_data)

        try:
            with open(self.session_file_path, 'w', encoding='utf-8') as f:
                json.dump(session_data, f, indent=4)
            print("Session saved successfully.")
        except Exception as e:
            print(f"Error saving session: {e}")

    def load_session(self) -> Dict[str, Any] | None:
        """Загружает данные сессии из session.json."""
        if not os.path.exists(self.session_file_path):
            return None

        try:
            with open(self.session_file_path, 'r', encoding='utf-8') as f:
                session_data = json.load(f)
            
            if 'root_path' in session_data or 'last_open_dir' in session_data:
                return session_data
            else:
                print("Session file is corrupt or incomplete.")
                return None

        except Exception as e:
            print(f"Error loading session: {e}")
            return None
            
    def restore_session(self):
        """Восстанавливает сохраненное состояние приложения при запуске."""
        session_data = self.load_session()
        if not session_data:
            return

        root_path = session_data.get('root_path')
        open_tabs_data = session_data.get('open_tabs', [])
        current_tab_index = session_data.get('current_tab_index', -1)
        foldouts = session_data.get('foldouts', {})
        last_open_dir = session_data.get('last_open_dir')
        
        # 1. Восстановление последнего пути для QFileDialog
        if last_open_dir and os.path.isdir(last_open_dir):
            self.parent_window._last_open_dir = last_open_dir
        
        # 2. Если нет открытого проекта, останавливаемся
        if not root_path or not os.path.isdir(root_path):
            return
            
        # 3. Восстановление состояния раскрытия папок
        self.parent_window._foldouts = foldouts

        # 4. Загрузка структуры папки 
        error_color = QColor(self.parent_window.STYLES['DarkTheme']['NotificationError'])
        success_color = QColor(self.parent_window.STYLES['DarkTheme']['NotificationSuccess'])
        
        new_structure = self.parent_window.lang_service.get_language_structure_from_path(root_path)

        if not self.parent_window.validator.validate_structure(new_structure):
            self.parent_window.show_notification(
                f"Session folder validation failed: {self.parent_window.validator.get_last_error()}", 
                error_color, 
                duration_ms=5000
            )
            return
            
        self.parent_window.temp_structure = new_structure
        self.parent_window.root_lang_path_normalized = self.parent_window.temp_structure.get('root_path')

        folder_name = os.path.basename(root_path)
        self.parent_window.language_label.setText(f"Language: {folder_name}")
        self.parent_window.show_notification(
            f"Structure loaded from previous session: {folder_name}", 
            success_color
        )

        # 5. Восстановление открытых вкладок
        for tab_data in open_tabs_data:
            file_path = tab_data.get('file_path')
            is_dirty = tab_data.get('is_dirty', False)
            dirty_content = tab_data.get('dirty_content')
            
            if not os.path.exists(file_path): continue
            
            file_content = ""
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    file_content = f.read()
            except Exception as e:
                print(f"Error reading file {os.path.basename(file_path)} during restore: {e}")
                continue

            is_valid_file = False
            for normalized_folder, files in self.parent_window.temp_structure['structure'].items():
                file_name = os.path.basename(file_path)
                if file_name in files and file_path.lower().startswith(normalized_folder): 
                     is_valid_file = True
                     break
            
            if is_valid_file:
                content_to_use = dirty_content if is_dirty and dirty_content is not None else file_content
                
                new_tab = YamlTab(file_path, content_to_use)
                new_tab.is_dirty = is_dirty 
                self.parent_window.open_tabs.append(new_tab)

        # 6. Установка активной вкладки
        if self.parent_window.open_tabs:
            target_index = min(current_tab_index, len(self.parent_window.open_tabs) - 1)
            
            if target_index < 0:
                 target_index = 0
                 
            self.parent_window.current_tab_index = target_index
            self.parent_window.current_tab = self.parent_window.open_tabs[target_index]
            self.parent_window.text_edit.setText(self.parent_window.current_tab.yaml_text)
            
            # ВОЗВРАЩЕНО: Используем clearUndoRedoStacks()
            self.parent_window.text_edit.document().clearUndoRedoStacks() 
        else:
            self.parent_window.current_tab_index = -1
            self.parent_window.current_tab = None
            self.parent_window.text_edit.setText("")
            # ВОЗВРАЩЕНО: Используем clearUndoRedoStacks()
            if hasattr(self.parent_window.text_edit, 'clearUndoRedoStacks'):
                 self.parent_window.text_edit.document().clearUndoRedoStacks()

        # 7. Обновление UI
        self.parent_window.draw_file_tree()
        self.parent_window.draw_tabs_placeholder()
        self.parent_window.update_status_bar()
        self.parent_window.update_undo_redo_ui()