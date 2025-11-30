# src/session_manager.py
import os
import json
import sys  
from typing import Dict, Any, List, TYPE_CHECKING
from PyQt5.QtGui import QColor 
from PyQt5.QtGui import QIcon, QPixmap

from models import YamlTab 

# For static type checking without circular import at runtime
if TYPE_CHECKING:
    from view import YAMLEditorWindow
    from PyQt5.QtWidgets import QTextEdit 

class SessionManager:
    """
    Manages saving and loading the editor's session state.
    Includes saving the last open path for QFileDialog.
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
        Saves the current application state to session.json.
        """
        current_tab_index = -1
        if self.parent_window.current_tab:
            try:
                current_tab_index = self.parent_window.open_tabs.index(self.parent_window.current_tab)
            except ValueError:
                current_tab_index = -1

        session_data = {
            'root_path': self.parent_window.root_lang_path_normalized,
            'root_localization_path': self.parent_window.root_localization_path,
            'active_language': self.parent_window.active_language,
            'current_tab_index': current_tab_index,
            'open_tabs': [], # Always initialize as empty list
            'foldouts': self.parent_window._foldouts,
            'last_open_dir': self.parent_window._last_open_dir
        }

        # If there's no root_localization_path, we still want to save last_open_dir
        if not self.parent_window.root_localization_path:
            if self.parent_window._last_open_dir:
                session_data['last_open_dir'] = self.parent_window._last_open_dir
            # If no root_localization_path and no last_open_dir, then nothing significant to save
            elif not self.parent_window.open_tabs: # Only return if no tabs are open either
                if os.path.exists(self.session_file_path):
                    try:
                        os.remove(self.session_file_path)
                    except Exception as e:
                        print(f"Warning: Could not clear session file: {e}")
                return

        # Always iterate and save open tabs if root_localization_path is available
        if self.parent_window.root_localization_path:
            normalized_root_localization_path = self.parent_window.lang_service.normalize_path(self.parent_window.root_localization_path)
            for tab in self.parent_window.open_tabs:
                normalized_tab_file_path = self.parent_window.lang_service.normalize_path(tab.file_path) # Normalize tab path
                # Check if the tab's file_path starts with the normalized root_localization_path
                if normalized_tab_file_path.startswith(normalized_root_localization_path):
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
        """Loads session data from session.json."""
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
        """Restores the saved application state on startup."""
        session_data = self.load_session()
        if not session_data:
            return

        root_path = session_data.get('root_path')
        root_localization_path = session_data.get('root_localization_path') # New: Load root localization path
        active_language = session_data.get('active_language') # New: Load active language
        open_tabs_data = session_data.get('open_tabs', [])
        current_tab_index = session_data.get('current_tab_index', -1)
        foldouts = session_data.get('foldouts', {})
        last_open_dir = session_data.get('last_open_dir')
        
        # 1. Restore last path for QFileDialog
        if last_open_dir and os.path.isdir(last_open_dir):
            self.parent_window._last_open_dir = last_open_dir
        
        # 2. If no project is open, stop
        if not root_path or not os.path.isdir(root_path):
            return
            
        # New: Restore root_localization_path and populate language selector
        if root_localization_path and os.path.isdir(root_localization_path):
            self.parent_window.root_localization_path = root_localization_path
            language_folders = self.parent_window.lang_service.get_language_folders(root_localization_path)
            self.parent_window.language_selector_combo.clear()
            if language_folders:
                for lang_code in language_folders:
                    flag_path = os.path.join(root_localization_path, lang_code, 'flag.png')
                    icon = QIcon()
                    if os.path.isfile(flag_path):
                        pix = QPixmap(flag_path)
                        if not pix.isNull():
                            icon = QIcon(pix)
                    self.parent_window.language_selector_combo.addItem(icon, lang_code.upper())

                self.parent_window.language_selector_combo.setEnabled(True)

                # If active_language from session is not set or not in the list (lowercase comparison)
                session_active_language_lower = active_language.lower() if active_language else ""
                if not session_active_language_lower or session_active_language_lower not in [lc.lower() for lc in language_folders]:
                    self.parent_window.active_language = language_folders[0].lower()
                else:
                    self.parent_window.active_language = session_active_language_lower
                
                self.parent_window.language_selector_combo.setCurrentText(self.parent_window.active_language.upper())
            else:
                self.parent_window.language_selector_combo.setEnabled(False)
                self.parent_window.active_language = None

        # 3. Restore folder foldout states
        self.parent_window._foldouts = foldouts

        # 4. Load folder structure 
        error_color = QColor(self.parent_window.STYLES['DarkTheme']['NotificationError'])
        success_color = QColor(self.parent_window.STYLES['DarkTheme']['NotificationSuccess'])
        
        # Use get_language_specific_structure to load the active language's structure
        new_structure = self.parent_window.lang_service.get_language_specific_structure(
            root_localization_path, self.parent_window.active_language
        )

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
        # self.parent_window.language_label.setText(f"Language: {folder_name}") # Removed
        self.parent_window.show_notification(
            f"Structure loaded from previous session: {folder_name}", 
            success_color
        )

        # 5. Restore open tabs
        for tab_data in open_tabs_data:
            file_path = tab_data.get('file_path')
            is_dirty = tab_data.get('is_dirty', False)
            dirty_content = tab_data.get('dirty_content')
            
            # Normalize the file_path from session_data for consistent comparison
            normalized_tab_file_path = self.parent_window.lang_service.normalize_path(file_path)

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
                # Use the normalized_tab_file_path for comparison
                if file_name in files and normalized_tab_file_path.startswith(normalized_folder): 
                     is_valid_file = True
                     break
            
            if is_valid_file:
                content_to_use = dirty_content if is_dirty and dirty_content is not None else file_content
                
                new_tab = YamlTab(file_path, content_to_use)
                new_tab.is_dirty = is_dirty 
                self.parent_window.open_tabs.append(new_tab)

        # 6. Set active tab
        if self.parent_window.open_tabs:
            target_index = min(current_tab_index, len(self.parent_window.open_tabs) - 1)
            
            if target_index < 0:
                 target_index = 0
                 
            self.parent_window.current_tab_index = target_index
            self.parent_window.current_tab = self.parent_window.open_tabs[target_index]
            self.parent_window.text_edit.setText(self.parent_window.current_tab.yaml_text)
            
            # RESTORED: Use clearUndoRedoStacks()
            self.parent_window.text_edit.document().clearUndoRedoStacks() 
        else:
            self.parent_window.current_tab_index = -1
            self.parent_window.current_tab = None
            self.parent_window.text_edit.setText("")
            # RESTORED: Use clearUndoRedoStacks()
            if hasattr(self.parent_window.text_edit, 'clearUndoRedoStacks'):
                 self.parent_window.text_edit.document().clearUndoRedoStacks()

        # 7. Update UI
        self.parent_window.draw_file_tree()
        self.parent_window.draw_tabs_placeholder()
        self.parent_window.update_status_bar()
        self.parent_window.update_undo_redo_ui()