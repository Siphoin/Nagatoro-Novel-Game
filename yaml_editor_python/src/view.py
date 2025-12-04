# src/view.py
import os
import sys
import yaml
import collections
from typing import Dict, Any, Optional

from PyQt5.QtWidgets import QMainWindow
from PyQt5.QtWidgets import QWidget
from PyQt5.QtWidgets import QVBoxLayout
from PyQt5.QtWidgets import QHBoxLayout
from PyQt5.QtWidgets import QSplitter
from PyQt5.QtWidgets import QTextEdit
from PyQt5.QtWidgets import QToolBar
from PyQt5.QtWidgets import QLabel
from PyQt5.QtWidgets import QAction
from PyQt5.QtWidgets import QStatusBar
from PyQt5.QtWidgets import QPushButton
from PyQt5.QtWidgets import QLineEdit
from PyQt5.QtWidgets import QSizePolicy
from PyQt5.QtWidgets import QFileDialog
from PyQt5.QtWidgets import QScrollArea
from PyQt5.QtWidgets import QMessageBox
from PyQt5.QtWidgets import QMenu
from PyQt5.QtWidgets import QComboBox

from PyQt5.QtGui import QIcon
from PyQt5.QtGui import QFont
from PyQt5.QtGui import QColor
from PyQt5.QtGui import QPixmap
from PyQt5.QtSvg import QSvgRenderer
from PyQt5.QtCore import QSize

from PyQt5.QtCore import Qt
from PyQt5.QtCore import QTimer
from PyQt5.QtCore import QCoreApplication
from PyQt5.QtCore import QByteArray
from PyQt5.QtCore import QUrl
from PyQt5.QtCore import QPropertyAnimation, QEasingCurve

from PyQt5.QtWidgets import QGraphicsOpacityEffect

# Imports for .models should be available if they are in the same src folder
from models import YamlTab
from highlighter import YamlHighlighter
from validator import StructureValidator
from session_manager import SessionManager # Add this if session_manager.py is in src/
from settings_manager import SettingsManager
from language_service import LanguageService # Import the LanguageService from the new file
from views.tabs import question_message_box # Import the custom message box function

# Progress dialog imports
from progress_dialog import OperationRunner
from apk_save_worker import ApkSaveWorker

# --- UTILITY FOR CREATING QIcon FROM SVG string ---
def create_icon_from_svg(svg_content: str, size: QSize = QSize(16, 16)) -> QIcon:
    """Creates a QIcon from SVG code using a data URI."""
    svg_bytes = QByteArray(svg_content.encode('utf-8'))
    base64_data = svg_bytes.toBase64().data().decode()
    data_uri = f'data:image:svg+xml;base64,{base64_data}'
    icon = QIcon(data_uri)
    return icon

# -------------------------------------------------------------------


class YAMLEditorWindow(QMainWindow):

    def _load_folder_icon(self) -> str:
        """Loads the folder SVG icon from file, fallback to default if not found."""
        try:
            icon_path = self._get_resource_path('icons/folder.svg')
            with open(icon_path, 'r', encoding='utf-8') as f:
                return f.read()
        except FileNotFoundError:
            # Fallback to default colored icon if file not found
            folder_color = self.STYLES['DarkTheme'].get('FolderIconColor', '#E06C75')
            return f"""
<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="{folder_color}" d="M14 6H8.5L7 4.5H2a1 1 0 0 0-1 1V11a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V7a1 1 0 0 0-1-1zM2 5h4.5l1 1h6.5v4H2V5z"/>
</svg>
"""

    def _load_yaml_file_icon(self) -> str:
        """Loads the YAML file SVG icon from file, fallback to default if not found."""
        try:
            icon_path = self._get_resource_path('icons/yaml_file.svg')
            with open(icon_path, 'r', encoding='utf-8') as f:
                return f.read()
        except FileNotFoundError:
            # Fallback to default colored icon if file not found
            yaml_color = self.STYLES['DarkTheme'].get('YamlFileIconColor', '#CCCCCC')
            return f"""
<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
  <path fill="{yaml_color}" d="M4 1h8a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1zM4 2v12h8V2H4z"/>
  <text x="8" y="10" font-family="Arial, sans-serif" font-size="8" font-weight="bold" fill="#007bff" text-anchor="middle">
    Y
  </text>
</svg>
"""

    def _load_android_icon(self) -> str:
        """Loads the Android SVG icon from file, fallback to default if not found."""
        try:
            icon_path = self._get_resource_path('icons/android.svg')
            with open(icon_path, 'r', encoding='utf-8') as f:
                return f.read()
        except FileNotFoundError:
            # Fallback to default Android icon if file not found
            android_color = self.STYLES['DarkTheme'].get('HighlightColor', '#C84B31')
            return f"""
<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24">
  <path fill="{android_color}" d="M17.66,11.2C17.43,10.9 17.15,10.64 16.89,10.38C16.22,9.78 15.46,9.3 14.82,8.96C14.71,8.9 14.59,8.85 14.47,8.8C12.55,7.84 10.45,7.84 8.53,8.8C8.41,8.85 8.29,8.9 8.18,8.96C7.54,9.3 6.78,9.78 6.11,10.38C5.85,10.64 5.57,10.9 5.34,11.2C5.25,11.33 5.17,11.46 5.1,11.6C4.92,11.95 4.79,12.32 4.72,12.7C4.65,13.08 4.63,13.47 4.65,13.86C4.67,14.25 4.74,14.63 4.85,15C5.04,15.66 5.36,16.26 5.76,16.77C6.16,17.28 6.64,17.69 7.17,18C7.6,18.23 8.05,18.4 8.51,18.5C8.97,18.6 9.44,18.64 9.91,18.61C10.38,18.58 10.84,18.48 11.27,18.32C11.7,18.16 12.09,17.94 12.44,17.66C12.79,17.94 13.18,18.16 13.61,18.32C14.04,18.48 14.5,18.58 14.97,18.61C15.44,18.64 15.91,18.6 16.37,18.5C16.83,18.4 17.28,18.23 17.68,18C18.21,17.69 18.69,17.28 19.09,16.77C19.49,16.26 19.81,15.66 20,15C20.11,14.63 20.18,14.25 20.2,13.86C20.22,13.47 20.2,13.08 20.13,12.7C20.06,12.32 19.93,11.95 19.75,11.6C19.68,11.46 19.6,11.33 19.51,11.2C19.28,10.9 19,10.64 18.74,10.38C18.48,10.12 18.22,9.86 17.96,9.6C17.89,9.54 17.81,9.48 17.73,9.42C17.66,9.36 17.59,9.3 17.52,9.24C17.72,9.6 17.82,9.99 17.82,10.38C17.82,10.64 17.78,10.89 17.66,11.2Z"/>
  <path fill="{android_color}" d="M9,16.75C8.31,16.75 7.75,16.19 7.75,15.5C7.75,14.81 8.31,14.25 9,14.25C9.69,14.25 10.25,14.81 10.25,15.5C10.25,16.19 9.69,16.75 9,16.75Z"/>
  <path fill="{android_color}" d="M15,16.75C14.31,16.75 13.75,16.19 13.75,15.5C13.75,14.81 14.31,14.25 15,14.25C15.69,14.25 16.25,14.81 16.25,15.5C16.25,16.19 15.69,16.75 15,16.75Z"/>
</svg>
"""

    def _update_svg_colors(self, svg_content: str, folder_color: str = None, yaml_color: str = None, excel_color: str = None) -> str:
        """Updates colors in SVG content based on current styles, replacing white with the specified color."""

        # For folder icon: if it contains the folder path and folder_color is specified, replace white with folder_color
        if folder_color and 'M2 4.5A1.5 1.5 0 0 1 3.5 3h3.086a1.5 1.5 0 0 1 1.06.44L8.56 4.354A1.5 1.5 0 0 0 9.62 4.8H12.5A1.5 1.5 0 0 1 14 6.3v5.2A1.5 1.5 0 0 1 12.5 13h-9A1.5 1.5 0 0 1 2 11.5v-7z' in svg_content:
            # Replace 'white' with the specified folder color in both the svg tag and path tags
            svg_content = svg_content.replace('fill="white"', f'fill="{folder_color}"')

        # For YAML file icon: if it contains the document path and yaml_color is specified, replace white with yaml_color
        if yaml_color and 'M4 2h5.5L13 5.5V13a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z' in svg_content:
            # Replace 'white' with the specified YAML color in all fill attributes
            svg_content = svg_content.replace('fill="white"', f'fill="{yaml_color}"')

        return svg_content

    def _create_icon_from_svg_content(self, svg_content: str, is_folder: bool = False, is_yaml: bool = False) -> QIcon:
        """Creates a QIcon from SVG content using QSvgRenderer for better compatibility."""
        try:
            # Create a pixmap with the desired size
            pixmap = QPixmap(16, 16)
            pixmap.fill(Qt.transparent)  # Make background transparent

            # Create a painter to draw on the pixmap
            from PyQt5.QtGui import QPainter
            painter = QPainter(pixmap)
            painter.setRenderHint(QPainter.Antialiasing, True)
            painter.setRenderHint(QPainter.SmoothPixmapTransform, True)

            # Create an SVG renderer and render to the painter
            from PyQt5.QtCore import QByteArray
            svg_bytes = QByteArray(svg_content.encode('utf-8'))
            renderer = QSvgRenderer(svg_bytes)
            renderer.render(painter)

            painter.end()

            # Return QIcon from the pixmap
            return QIcon(pixmap)
        except Exception:
            # Fallback to the original method if SVG rendering fails
            return create_icon_from_svg(svg_content)

    def __init__(self):
        super().__init__()

        # --- Styles Initialization ---
        self.STYLES = self._load_styles()
        self.CSS_STYLES = self._generate_css(self.STYLES)

        self.setWindowTitle("YAML Editor")
        self.setGeometry(100, 100, 1200, 800)
        self.setStyleSheet(self.CSS_STYLES) # Apply generated styles

        # --- Icons Initialization ---
        folder_icon_color = self.STYLES['DarkTheme'].get('FolderIconColor', '#E06C75')
        yaml_icon_color = self.STYLES['DarkTheme'].get('YamlFileIconColor', '#CCCCCC')

        # Load SVG contents
        folder_svg_content = self._load_folder_icon()
        yaml_svg_content = self._load_yaml_file_icon()
        android_svg_content = self._load_android_icon()

        # Apply color updates based on loaded styles
        folder_svg_content = self._update_svg_colors(folder_svg_content, folder_color=folder_icon_color)
        yaml_svg_content = self._update_svg_colors(yaml_svg_content, yaml_color=yaml_icon_color)
        # Android icon will use the highlight color from theme

        # Create icons with applied colors
        self.icon_folder = self._create_icon_from_svg_content(folder_svg_content)
        self.icon_yaml = self._create_icon_from_svg_content(yaml_svg_content)
        self.icon_android = self._create_icon_from_svg_content(android_svg_content, is_folder=False, is_yaml=False)
        self._last_open_dir: str = os.path.expanduser("~")

        # --- Model/Services ---
        self.lang_service = LanguageService()
        self.validator = StructureValidator() # <-- VALIDATOR INITIALIZATION
        self.temp_structure = {'root_path': None, 'structure': {}}
        self.open_tabs: list[YamlTab] = []
        self.current_tab_index = -1
        self.current_tab: YamlTab | None = None

        # UI Variables (like in C#)
        self.root_lang_path_normalized: str | None = None
        self.root_localization_path: str | None = None # New: Stores the root folder containing language subdirectories
        self.active_language: str | None = None # New: Stores the currently selected language (e.g., "en", "ru")
        self.language_selector_combo = None # New: QComboBox for language selection
        self._notification_timer = QTimer(self)
        self._notification_timer.timeout.connect(self.hide_notification)
        self._notification_label: QLabel | None = None
        self._current_font_size = 14
        self._foldouts: Dict[str, bool] = {} # For storing folder states

        # Устанавливаем иконку для главного окна
        icon_path = self._get_resource_path('icons/app.svg')
        try:
            # Загружаем SVG иконку и устанавливаем как иконку окна
            with open(icon_path, 'r', encoding='utf-8') as f:
                svg_content = f.read()
            # Создаем иконку из SVG
            icon = self._create_icon_from_svg_content(svg_content)
            if icon:
                self.setWindowIcon(icon)
        except Exception:
            # Если не удалось загрузить SVG иконку, используем стандартную иконку
            pass

        self.settings_manager = SettingsManager()

        self.init_ui()
        self.update_status_bar()

        self.session_manager = SessionManager(self)
        self.session_manager.restore_session() # Restore session on startup

        # Initialize auto-save timer
        self.auto_save_timer = QTimer(self)
        self.auto_save_timer.timeout.connect(self.perform_auto_save)
        self._setup_auto_save_timer()

    def closeEvent(self, event):
        """
        Intercepts the window close event.
        First checks for unsaved changes, then saves the session.
        """

        # Check if there are tabs with unsaved changes
        dirty_tab = next((t for t in self.open_tabs if t.is_dirty), None)

        if dirty_tab:
            reply = question_message_box(self, 'Unsaved Changes',
                "You have unsaved changes. Do you want to save all files before quitting?",
                QMessageBox.Save | QMessageBox.Discard | QMessageBox.Cancel, QMessageBox.Cancel)

            if reply == QMessageBox.Save:
                # Attempt to save all unsaved files
                for tab in [t for t in self.open_tabs if t.is_dirty]:
                    self.save_file_action(tab)

                # If there are still unsaved changes after attempting to save (e.g., due to YAML syntax error), cancel close.
                if any(t.is_dirty for t in self.open_tabs):
                    event.ignore()
                    return

            elif reply == QMessageBox.Cancel:
                event.ignore() # Cancel close
                return

        # If closing is allowed (no unsaved changes or user pressed Discard/Save)
        self.session_manager.save_session() # Save session state

        # Clean up temporary APK directory if it exists
        if hasattr(self, 'is_apk_mode') and self.is_apk_mode and hasattr(self, 'apk_temp_dir'):
            import shutil
            try:
                shutil.rmtree(self.apk_temp_dir, ignore_errors=True)
            except Exception:
                pass  # Ignore errors during cleanup

        event.accept()

    def _setup_auto_save_timer(self):
        """Setup the auto-save timer based on current settings"""
        # Stop the timer if it's running
        self.auto_save_timer.stop()

        # Check if auto-save is enabled
        if self.settings_manager.auto_save_enabled:
            # Set the interval from settings (convert from seconds to milliseconds)
            interval_ms = self.settings_manager.auto_save_interval * 1000
            self.auto_save_timer.setInterval(interval_ms)
            self.auto_save_timer.start()

    def perform_auto_save(self):
        """Perform auto-save of all dirty tabs"""
        if not self.settings_manager.auto_save_enabled:
            return

        # Save all open tabs that have unsaved changes
        dirty_tabs = [t for t in self.open_tabs if t.is_dirty and t.file_path]
        if dirty_tabs:
            for tab in dirty_tabs:
                try:
                    with open(tab.file_path, 'w', encoding='utf-8') as f:
                        f.write(tab.yaml_text)
                    tab.is_dirty = False  # Mark as saved

                    # Update the UI to reflect the saved state
                    self.update_status_bar()
                except Exception as e:
                    print(f"Auto-save failed for {tab.file_path}: {e}")
                    # Don't stop auto-saving other files if one fails
                    continue

    def _get_resource_path(self, relative_path: str) -> str:
        """
        Gets the path to a resource.
        In EXE mode, the file structure is based on PyInstaller --add-data specification.
        """
        if getattr(sys, 'frozen', False):
            # In PyInstaller (EXE) mode
            base_path = os.path.dirname(sys.executable)

            # Check in executable directory (where --add-data puts files)
            full_path = os.path.join(base_path, relative_path)
            if os.path.exists(full_path):
                return full_path

            # Check in PyInstaller temp directory
            try:
                temp_path = sys._MEIPASS
                temp_full_path = os.path.join(temp_path, relative_path)
                if os.path.exists(temp_full_path):
                    return temp_full_path
            except AttributeError:
                # _MEIPASS not available, skip this check
                pass

            # If neither worked, default to base path
            return full_path
        else:
            # In development mode: styles.yaml is next to view.py (in the src folder)
            base_path = os.path.dirname(os.path.abspath(__file__))
            return os.path.join(base_path, relative_path)

    def _load_styles(self) -> Dict[str, Any]:
        """Loads styles from styles.yaml or uses backups."""

        # KEY CHANGE: Use _get_resource_path to access the file
        # Since in PyInstaller we specified --add-data "src/styles.yaml;src",
        # the file will be available in the 'src' folder relative to the PyInstaller base path.
        # But since view.py itself is in 'src' in development mode,
        # we just need to access the 'styles.yaml' file in our folder.

        # If view.py is in src/, and styles.yaml is next to it:

        # OLD CODE:
        # style_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'styles.yaml')

        # NEW CODE (Uses a path that works in PyInstaller and in development)
        style_file = self._get_resource_path('styles.yaml')

        # --- BACKUP STYLES (Updated for red-black theme) ---
        default_styles = {
            # ... (Your styles) ...
            'DarkTheme': {
                'Background': "#1A1A1A", 'Foreground': "#E8E8E8", 'SecondaryBackground': "#2A2A2A",
                'EditorBackground': "#1F1F1F", 'BorderColor': "#3A3A3A", 'HighlightColor': "#C84B31",
                'HoverColor': "#3A3A3A", 'FilePanelBackground': "#181818", 'FilePanelHover': "#2D2D2D",
                'FolderColor': "#E06C75", 'StatusDefault': "#999999", 'NotificationSuccess': "#6BA878",
                'NotificationError': "#D9685A",
                'NotificationWarning': "#E8C56B",
                'FolderIconColor': "#E06C75",  # Color for the folder icon
                'YamlFileIconColor': "#CCCCCC"  # Color for YAML file icon
            }
        }

        # ... (Rest of file loading logic) ...
        try:
            if os.path.exists(style_file):
                 # ... (Rest of loading logic) ...
                with open(style_file, 'r', encoding='utf-8') as f:
                    styles = yaml.safe_load(f)
                    # ... (Key check and return) ...
                    if styles and 'DarkTheme' in styles:
                        return styles
                    else:
                        print("Warning: styles.yaml found, but missing 'DarkTheme' key. Using default styles.")
                        return default_styles
            else:
                 print(f"Warning: styles.yaml not found at {style_file}. Using default styles.")
                 return default_styles
        except Exception as e:
            print(f"Error loading styles.yaml ({e}). Using default styles.")
            return default_styles

    # ... (Rest of _generate_css) ...
    def _generate_css(self, styles: Dict[str, Any]) -> str:
        """Generates a CSS string based on loaded styles."""
        theme = styles.get('DarkTheme', {})

        css = f"""
        QMainWindow, QWidget {{ background-color: {theme.get('Background')}; color: {theme.get('Foreground')}; }}
        QSplitter::handle {{ background-color: {theme.get('Background')}; }}

        /* TOOLBAR & ACTIONS (Fixed visual bugs of QAction buttons) */
        QToolBar {{
            background-color: {theme.get('SecondaryBackground')};
            spacing: 5px;
            border-bottom: 1px solid {theme.get('Background')};
        }}
        QToolButton {{
            color: {theme.get('Foreground')};
            background-color: {theme.get('SecondaryBackground')};
            border: 1px solid {theme.get('SecondaryBackground')};
            padding: 3px 6px;
        }}
        QToolButton:hover {{ background-color: {theme.get('HoverColor')}; border: 1px solid {theme.get('HoverColor')}; }}
        QToolButton:pressed {{ background-color: {theme.get('Background')}; }}

        /* File Panel */
        QWidget#FilePanelWidget {{ background-color: {theme.get('FilePanelBackground')}; }}
        QScrollArea {{ border: none; }}

        QPushButton {{ color: {theme.get('Foreground')}; background-color: {theme.get('FilePanelBackground')}; border: none; }}
        QPushButton:hover {{ background-color: {theme.get('FilePanelHover')}; }}

        /* Editor */
        QLineEdit {{
            background-color: {theme.get('EditorBackground')};
            color: {theme.get('Foreground')};
            border: 1px solid {theme.get('BorderColor')};
            padding: 2px;
        }}
        QTextEdit {{
            background-color: {theme.get('EditorBackground')};
            color: {theme.get('Foreground')};
            border: 1px solid {theme.get('NotificationError')};
            padding: 2px;
        }}

        /* Tab Bar */
        QToolBar#TabPlaceholder {{ background-color: {theme.get('Background')}; border: none; }}

        /* Status Bar */
        QStatusBar {{
            background-color: {theme.get('SecondaryBackground')};
            border-top: 1px solid {theme.get('Background')};
            color: {theme.get('StatusDefault')};
        }}

        /* Context Menu */
        QMenu {{
            background-color: {theme.get('SecondaryBackground')};
            color: {theme.get('Foreground')};
            border: 1px solid {theme.get('NotificationError')};
            border-radius: 4px;
        }}
        QMenu::item {{
            padding: 5px 15px 5px 25px; /* Add padding for better spacing */
            background-color: transparent;
        }}
        QMenu::item:selected {{
            background-color: {theme.get('HoverColor')};
            color: {theme.get('HighlightColor')};
        }}
        QMenu::separator {{
            height: 1px;
            background-color: {theme.get('BorderColor')};
            margin: 4px 5px;
        }}
        """
        return css

    def init_ui(self):
        # ... (init_ui code) ...
        self.create_main_toolbar()

        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)

        self.splitter = QSplitter(Qt.Horizontal)

        self.file_panel = self.create_file_panel()
        self.splitter.addWidget(self.file_panel)

        self.editor_area = self.create_editor_area()
        self.splitter.addWidget(self.editor_area)

        self.splitter.setSizes([250, 950])

        main_layout.addWidget(self.splitter)

        self.create_status_bar()

        # Removed: new bottom_layout with language selector
        # bottom_layout = QHBoxLayout()
        # bottom_layout.addStretch(1)
        # bottom_layout.addWidget(self.language_selector_combo)
        # bottom_layout.addStretch(1)
        # main_layout.addLayout(bottom_layout) # Add to the main_layout (QVBoxLayout) which is in the central widget


    def create_main_toolbar(self):
        from views.toolbar import create_main_toolbar as _create_main_toolbar
        return _create_main_toolbar(self)

    def change_active_language(self, index: int):
        """Changes the currently active language based on dropdown selection."""
        if self.language_selector_combo and index >= 0:
            selected_language_upper = self.language_selector_combo.itemText(index)
            selected_language_lower = selected_language_upper.lower()

            if selected_language_lower != self.active_language:
                self.active_language = selected_language_lower

                # Clear open tabs, but don't reload the entire structure here.
                self.open_tabs.clear()
                self.current_tab_index = -1
                self.current_tab = None
                if hasattr(self, 'text_edit') and self.text_edit:
                    self.text_edit.setPlainText("")
                    if hasattr(self.text_edit, 'document'):
                        self.text_edit.document().clearUndoRedoStacks()

                # Reload the language-specific structure for the new active language
                if self.root_localization_path and self.active_language:
                    self.temp_structure = self.lang_service.get_language_specific_structure(
                        self.root_localization_path, self.active_language
                    )
                    self.root_lang_path_normalized = self.temp_structure.get('root_path')

                # Simply redraw UI elements based on new active language
                self.draw_file_tree()
                self.draw_tabs_placeholder()
                self.update_status_bar()
                self.update_undo_redo_ui()

                color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
                self.show_notification(f"Active language set to: {self.active_language.upper()}", color)


    def open_folder_dialog(self):
        """Opens a dialog for selecting the root localization folder."""
        initial_dir = self._last_open_dir if self._last_open_dir and os.path.isdir(self._last_open_dir) else os.path.expanduser("~")
        folder_path = QFileDialog.getExistingDirectory(self, "Select Language Root Folder", initial_dir)

        if folder_path:
            self._last_open_dir = folder_path
            self.root_localization_path = folder_path # Store the root localization path
            self.reload_language_structure(folder_path)

    def open_apk_dialog(self):
        """Opens an APK file and extracts StreamingAssets/Language folder."""
        import zipfile
        import tempfile
        import os
        import shutil

        # Open file dialog to select APK
        initial_dir = self._last_open_dir if self._last_open_dir and os.path.isdir(self._last_open_dir) else os.path.expanduser("~")
        apk_path, _ = QFileDialog.getOpenFileName(
            self,
            "Select APK File",
            initial_dir,
            "APK Files (*.apk)"
        )

        if not apk_path:
            return  # User cancelled the dialog

        # Track the APK path for potential re-packaging later
        self.current_apk_path = apk_path

        # Create a temporary directory to extract the APK contents
        temp_dir = tempfile.mkdtemp(prefix="apk_extract_")

        try:
            with zipfile.ZipFile(apk_path, 'r') as apk_zip:
                # Find all files in StreamingAssets/Language directory
                language_files = []
                for file_info in apk_zip.filelist:
                    if file_info.filename.startswith('assets/StreamingAssets/Language/') and not file_info.is_dir():
                        language_files.append(file_info)

                if not language_files:
                    # Try alternative path commonly used in Unity games
                    language_files = []
                    for file_info in apk_zip.filelist:
                        if (file_info.filename.startswith('StreamingAssets/Language/') or
                            file_info.filename.startswith('root/assets/StreamingAssets/Language/')) and not file_info.is_dir():
                            language_files.append(file_info)

                if not language_files:
                    # Try the path found in NagatoroNovelGame: assets/Language/
                    language_files = []
                    for file_info in apk_zip.filelist:
                        if file_info.filename.startswith('assets/Language/') and not file_info.is_dir():
                            language_files.append(file_info)

                if not language_files:
                    color = QColor(self.STYLES['DarkTheme']['NotificationError'])
                    self.show_notification("No language files found in APK", color)
                    return

                # Extract language files to temp directory
                for file_info in language_files:
                    # Determine the relative path within the language directory
                    relative_path = file_info.filename
                    # Extract to temp directory while preserving folder structure
                    extracted_path = os.path.join(temp_dir, relative_path)

                    # Create directory if it doesn't exist
                    os.makedirs(os.path.dirname(extracted_path), exist_ok=True)

                    # Extract the file
                    with open(extracted_path, 'wb') as output_file:
                        output_file.write(apk_zip.read(file_info))

                # Determine the correct language root path based on actual directory structure
                # Check if we have direct language folders in the extracted root
                lang_root_path = temp_dir

                # Check if the languages are under assets/Language/
                possible_lang_paths = [
                    os.path.join(temp_dir, 'assets', 'Language'),
                    os.path.join(temp_dir, 'assets', 'StreamingAssets', 'Language'),
                    os.path.join(temp_dir, 'StreamingAssets', 'Language'),
                    os.path.join(temp_dir, 'root', 'assets', 'StreamingAssets', 'Language')
                ]

                for path in possible_lang_paths:
                    if os.path.exists(path):
                        lang_root_path = path
                        break

                # Set the detected language root path as our localization path
                self.root_localization_path = lang_root_path
                self.is_apk_mode = True  # Flag to indicate we're working with APK content
                self.apk_temp_dir = temp_dir  # Store reference to temp directory (not language root)

                # Reload language structure with the correct path
                self.reload_language_structure(lang_root_path)

                color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
                self.show_notification(f"APK loaded: {os.path.basename(apk_path)}", color)

        except zipfile.BadZipFile:
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification("Invalid APK file", color)
            # Clean up temp directory if extraction failed
            shutil.rmtree(temp_dir, ignore_errors=True)
        except Exception as e:
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification(f"Error opening APK: {str(e)}", color)
            # Clean up temp directory if extraction failed
            shutil.rmtree(temp_dir, ignore_errors=True)

    def save_changes_to_apk(self):
        """Saves changes back to the APK file if in APK mode using background thread."""
        # Check if we're in APK mode
        if not hasattr(self, 'is_apk_mode') or not self.is_apk_mode:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification("Not in APK mode", color)
            return

        if not hasattr(self, 'current_apk_path') or not self.current_apk_path:
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification("No APK file to save to", color)
            return

        # First, save any unsaved changes in open tabs
        unsaved_tabs = [t for t in self.open_tabs if t.is_dirty]
        if unsaved_tabs:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification(f"Saving {len(unsaved_tabs)} modified files before APK update...", color)

            for tab in unsaved_tabs:
                self.save_file_action(tab)

            # Check again if there are still unsaved changes after attempting to save
            still_unsaved = [t for t in self.open_tabs if t.is_dirty]
            if still_unsaved:
                color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
                self.show_notification(f"{len(still_unsaved)} files could not be saved", color)
                return
            else:
                color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
                self.show_notification("All modified files saved", color)
        else:
            color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
            self.show_notification("No modified files to save", color)

        # Confirm with user before overwriting APK
        reply = question_message_box(self, 'Save to APK',
            f"Save changes back to '{os.path.basename(self.current_apk_path)}'?\nThis will overwrite the original APK.",
            QMessageBox.Save | QMessageBox.Cancel, QMessageBox.Cancel)

        if reply != QMessageBox.Save:
            return

        # Run the APK saving in background thread
        from PyQt5.QtCore import QTimer, QThread

        def handle_apk_save_result(success, message):
            """Handle the result of APK saving operation"""
            if success:
                color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
                self.show_notification(message, color)
            else:
                # If saving failed, try to restore from backup
                backup_path = self.current_apk_path + ".backup"
                if os.path.exists(backup_path):
                    try:
                        import shutil
                        shutil.move(backup_path, self.current_apk_path)
                        color = QColor(self.STYLES['DarkTheme']['NotificationError'])
                        self.show_notification(f"Error saving APK, original restored: {message}", color)
                    except Exception:
                        color = QColor(self.STYLES['DarkTheme']['NotificationError'])
                        self.show_notification(f"Error saving APK and restoring backup: {message}", color)
                else:
                    color = QColor(self.STYLES['DarkTheme']['NotificationError'])
                    self.show_notification(f"Error saving APK: {message}", color)

        # Create a custom runner that handles the result
        from progress_dialog import ProgressDialog

        class CustomApkRunner(OperationRunner):
            def __init__(self, parent_window, operation_name="Operation"):
                super().__init__(parent_window, operation_name)

            def run_with_progress(self, worker_class, *args, **kwargs):
                # Create progress dialog with styles from parent window if available
                styles = getattr(self.parent_window, 'STYLES', {}) if hasattr(self.parent_window, 'STYLES') else {}
                self.dialog = ProgressDialog(self.parent_window, f"{self.operation_name} in Progress", styles=styles)

                # Create worker and thread
                self.worker = worker_class(*args, **kwargs)
                self.thread = QThread()

                # Move worker to thread
                self.worker.moveToThread(self.thread)

                # Connect signals
                self.worker.progress_updated.connect(self.dialog.update_progress)
                self.worker.status_updated.connect(self.dialog.update_status)
                self.worker.operation_finished.connect(handle_apk_save_result)  # Connect to our handler
                self.worker.operation_finished.connect(lambda success, message: self.dialog.close() if self.dialog else None)
                # Connect dialog cancel button to both cancel the worker and give immediate UI feedback
                self.dialog.cancel_button.clicked.connect(self._on_dialog_cancel)
                # Also directly connect to the worker's cancel method to ensure cancellation
                self.dialog.cancel_button.clicked.connect(lambda: self.worker.cancel() if hasattr(self.worker, 'cancel') else None)

                # Start thread and operation
                self.thread.started.connect(self.worker.run_operation)
                self.dialog.show()
                self.thread.start()

            def _on_dialog_cancel(self):
                """Handle dialog cancel button click with immediate feedback"""
                # Disable the cancel button to indicate cancellation is in progress
                if self.dialog and self.dialog.cancel_button:
                    self.dialog.cancel_button.setEnabled(False)
                    self.dialog.cancel_button.setText("Cancelling...")

        # Calculate the relative path for language files within the APK
        # This is the path from the APK root to the language directory
        lang_relative_path = ""
        if hasattr(self, 'apk_temp_dir') and hasattr(self, 'root_localization_path'):
            lang_relative_path = os.path.relpath(self.root_localization_path, self.apk_temp_dir).replace('\\', '/')

        runner = CustomApkRunner(self, "APK Saving")
        runner.run_with_progress(ApkSaveWorker,
                                self.current_apk_path,
                                getattr(self, 'apk_temp_dir', ''),
                                getattr(self, 'root_localization_path', ''),
                                lang_relative_path)


    def reload_language_structure(self, folder_path: str):
        """Reloads file structure from the selected folder."""

        dirty_tab = next((t for t in self.open_tabs if t.is_dirty), None)

        if dirty_tab:
            reply = question_message_box(self, 'Unsaved Changes',
                f"Do you want to save changes to file '{os.path.basename(dirty_tab.file_path)}' before reloading the structure? Changes will be lost for other unsaved files.",
                QMessageBox.Save | QMessageBox.Discard | QMessageBox.Cancel, QMessageBox.Cancel)

            if reply == QMessageBox.Save:
                for tab in [t for t in self.open_tabs if t.is_dirty]:
                    self.save_file_action(tab)
            elif reply == QMessageBox.Cancel:
                return

        # Clear open tabs (always when structure is reloaded, regardless of language change source)
        self.open_tabs.clear()
        self.current_tab_index = -1
        self.current_tab = None

        # Clear the text editor as well to match behavior when changing languages
        if hasattr(self, 'text_edit') and self.text_edit:
            self.text_edit.setPlainText("")
            if hasattr(self.text_edit, 'document'):
                self.text_edit.document().clearUndoRedoStacks()

        # New: Detect language folders and populate the dropdown
        language_folders = self.lang_service.get_language_folders(folder_path)
        self.language_selector_combo.clear()
        if language_folders:
            for lang_code in language_folders:
                flag_path = os.path.join(folder_path, lang_code, 'flag.png')
                icon = QIcon()
                if os.path.isfile(flag_path):
                    pix = QPixmap(flag_path)
                    if not pix.isNull():
                        icon = QIcon(pix)
                self.language_selector_combo.addItem(icon, lang_code.upper()) # Display uppercase

            self.language_selector_combo.setEnabled(True)

            current_active_language_lower = self.active_language if self.active_language else ""
            if current_active_language_lower not in [lc.lower() for lc in language_folders]:
                self.active_language = language_folders[0].lower() # Store lowercase

            self.language_selector_combo.setCurrentText(self.active_language.upper()) # Display uppercase

        else:
            self.language_selector_combo.setEnabled(False)
            self.active_language = None
            self.temp_structure = {'root_path': None, 'structure': {}}
            self.root_lang_path_normalized = None
            self.draw_file_tree()
            self.draw_tabs_placeholder()
            self.update_status_bar()
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification("No language folders found in the selected directory.", color)
            return

        # Now, load the structure specifically for the active language
        self.temp_structure = self.lang_service.get_language_specific_structure(
            folder_path, self.active_language
        )
        self.root_lang_path_normalized = self.temp_structure.get('root_path')

        # --- VALIDATION CHECK ---
        if not self.validator.validate_structure(self.temp_structure):
            self.temp_structure = {'root_path': None, 'structure': {}}
            self.root_lang_path_normalized = None
            self.draw_file_tree()
            self.draw_tabs_placeholder()
            self.update_status_bar()
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            error_msg = self.validator.get_last_error()
            self.show_notification(f"Validation failed for {os.path.basename(folder_path)}: {error_msg}", color, duration_ms=5000)
            return

        # If valid
        # self.temp_structure is already updated by get_language_specific_structure
        # self.root_lang_path_normalized is already updated

        if self.root_lang_path_normalized and self.temp_structure.get('structure'):
            self.draw_file_tree()
            self.draw_tabs_placeholder()
            self.update_status_bar()
            color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
            self.show_notification(f"Structure loaded and validated from: {os.path.basename(folder_path)}", color)
        else:
            self.draw_file_tree()
            self.draw_tabs_placeholder()
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification("Failed to load structure. Folder not found or empty.", color)

    # --- Validation ---
    def validate_structure(self):
        from views.validation import validate_structure as _validate_struct
        return _validate_struct(self)
    def validate_yaml(self, file_path: str, yaml_text: str) -> bool:
        from views.validation import validate_yaml as _validate
        return _validate(self, file_path, yaml_text)

    # ... (Rest of File Panel: create_file_panel, clear_layout, draw_file_tree, foldout methods) ...
    def create_file_panel(self) -> QWidget:
        from views.file_panel import create_file_panel as _create_file_panel
        return _create_file_panel(self)

    def clear_layout(self, layout):
        from views.file_panel import clear_layout as _clear_layout
        return _clear_layout(self, layout)

    def draw_file_tree(self):
        from views.file_panel import draw_file_tree as _draw_file_tree
        return _draw_file_tree(self)

    def get_or_set_foldout(self, path: str, default: bool = False) -> bool:
        from views.file_panel import get_or_set_foldout as _get_or_set_foldout
        return _get_or_set_foldout(self, path, default)

    def set_foldout(self, path: str, state: bool):
        from views.file_panel import set_foldout as _set_foldout
        return _set_foldout(self, path, state)

    def check_folder_for_match_recursive(self, folder_path_normalized: str) -> bool:
        from views.file_panel import check_folder_for_match_recursive as _check
        return _check(self, folder_path_normalized)

    def draw_folder_content(self, folder_path: str, structure: dict, level: int):
        from views.file_panel import draw_folder_content as _draw_folder_content
        return _draw_folder_content(self, folder_path, structure, level)


    def _add_file_button(self, name: str, path: str, level: int):
        from views.file_panel import _add_file_button as _add_btn
        return _add_btn(self, name, path, level)

    def load_file(self, file_path: str):
        from views.file_ops import load_file as _load
        return _load(self, file_path)

    def try_switch_file_action(self, new_file_path: str):
        from views.file_ops import try_switch_file_action as _try_switch
        return _try_switch(self, new_file_path)


    def create_editor_area(self) -> QWidget:
        from views.editor import create_editor_area as _create_editor_area
        return _create_editor_area(self)

    def create_editor_toolbar(self):
        from views.editor import create_editor_toolbar as _create_editor_toolbar
        return _create_editor_toolbar(self)


    def create_status_bar(self):
        self.status_bar = QStatusBar()
        self.setStatusBar(self.status_bar)
        self.status_label = QLabel("Ready.")
        self.status_bar.addWidget(self.status_label)

        self.language_selector_combo = QComboBox(self)
        self.language_selector_combo.setToolTip("Select active language")
        self.language_selector_combo.currentIndexChanged.connect(self.change_active_language)
        self.language_selector_combo.setEnabled(False)
        self.status_bar.addPermanentWidget(self.language_selector_combo)

        # Add font size label to status bar
        # Use settings manager value if available, otherwise default
        initial_font_size = 14
        if hasattr(self, 'settings_manager') and self.settings_manager:
            initial_font_size = self.settings_manager.font_size
        self.font_size_label = QLabel(f"Font Size: {initial_font_size} (Ctrl+↑/↓)")
        self.status_bar.addPermanentWidget(self.font_size_label)

        self._notification_label = QLabel()
        self._notification_label.setVisible(False)
        self.status_bar.addPermanentWidget(self._notification_label)

    def show_notification(self, message: str, color: QColor, duration_ms: int = 3000):
        from views.notifications import show_notification as _show
        return _show(self, message, color, duration_ms)

    def hide_notification(self):
        from views.notifications import hide_notification as _hide
        return _hide(self)


    def draw_tabs_placeholder(self):
        from views.tabs import draw_tabs_placeholder as _draw_tabs
        return _draw_tabs(self)


    def switch_tab_action(self, index):
        from views.tabs import switch_tab_action as _switch
        return _switch(self, index)

    def handle_text_change(self):
        from views.tabs import handle_text_change as _handle
        return _handle(self)


    def try_close_tab(self, index: int):
        """
        Handles request to close a tab by index,
        checking for unsaved changes and updating QTextEdit content.

        FIX: Ensures that after tab deletion, the input field
        is updated with the content of the new active tab or cleared.
        """

        from views.tabs import try_close_tab as _try_close
        return _try_close(self, index)

    def update_status_bar(self):
        # ... (update_status_bar code) ...
        status = "Ready."
        if self.current_tab and self.current_tab.is_dirty:
            status = f"Unsaved changes in {os.path.basename(self.current_tab.file_path)}*"
        self.status_label.setText(status)

    def update_undo_redo_ui(self):
        from views.tabs import update_undo_redo_ui as _update
        return _update(self)


    def save_file_action(self, tab_to_save):
        from views.file_ops import save_file_action as _save
        return _save(self, tab_to_save)

    def reload_file_action(self):
        from views.file_ops import reload_file_action as _reload
        return _reload(self)


    def reload_structure_action(self):
        # Use the root localization path instead of the current language folder path
        if self.root_localization_path:
            self.reload_language_structure(self.root_localization_path)
        else:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification("No folder open to reload.", color)

    def handle_undo(self):
        from views.shortcuts import handle_undo as _undo
        return _undo(self)

    def handle_redo(self):
        from views.shortcuts import handle_redo as _redo
        return _redo(self)


    def keyPressEvent(self, event):
        from views.shortcuts import keyPressEvent as _key
        return _key(self, event)

    def regenerate_language_manifest(self):
        """Regenerate the language manifest file based on language folders"""
        from language_manifest_generator import regenerate_language_manifest
        return regenerate_language_manifest(self)

    def create_new_language(self):
        """Create a new language from an existing template"""
        from language_creator import create_new_language_dialog
        return create_new_language_dialog(self)

    def open_styles_editor(self):
        """Открывает редактор стилей"""
        from views.styles_editor import StylesEditorDialog

        def on_styles_changed(styles):
            # Обновляем стили в основном окне
            updated_theme = {'DarkTheme': styles}
            self.STYLES = updated_theme
            self.CSS_STYLES = self._generate_css(updated_theme)
            self.setStyleSheet(self.CSS_STYLES)

            # Обновляем иконки, т.к. цвета иконок могут измениться
            folder_icon_color = self.STYLES['DarkTheme'].get('FolderIconColor', '#E06C75')
            yaml_icon_color = self.STYLES['DarkTheme'].get('YamlFileIconColor', '#CCCCCC')

            # Загружаем SVG иконки и обновляем их цвета
            folder_svg_content = self._load_folder_icon()
            yaml_svg_content = self._load_yaml_file_icon()
            android_svg_content = self._load_android_icon()

            # Обновляем цвета в SVG содержимом
            folder_svg_content = self._update_svg_colors(folder_svg_content, folder_color=folder_icon_color)
            yaml_svg_content = self._update_svg_colors(yaml_svg_content, yaml_color=yaml_icon_color)
            # Android icon will use the highlight color from theme

            # Пересоздаем иконки с новыми цветами
            self.icon_folder = self._create_icon_from_svg_content(folder_svg_content)
            self.icon_yaml = self._create_icon_from_svg_content(yaml_svg_content)
            self.icon_android = self._create_icon_from_svg_content(android_svg_content, is_folder=False, is_yaml=False)

            # Обновляем подсветку синтаксиса для текущего текстового редактора
            self.update_highlighter_colors(styles)

            # Обновляем цвета частиц в редакторе
            if hasattr(self, 'text_edit') and hasattr(self.text_edit, 'update_particle_colors'):
                self.text_edit.update_particle_colors({'DarkTheme': styles})

            # Перерисовываем дерево файлов, чтобы обновить иконки
            if hasattr(self, 'draw_file_tree'):
                self.draw_file_tree()

        dialog = StylesEditorDialog(self, self._get_resource_path('styles.yaml'))
        dialog.styles_changed.connect(on_styles_changed)
        dialog.exec_()


    def open_settings_dialog(self):
        """Opens the editor settings dialog"""
        from settings_dialog import SettingsDialog

        def on_settings_changed(settings):
            # Update settings in the main window
            # Check if the line numbers option changed
            if 'show_line_numbers' in settings:
                # Can update line numbers display in the future
                pass

            # Check if font settings changed
            if 'font_family' in settings or 'font_size' in settings:
                # Update the font in the current text editor if it exists
                if hasattr(self, 'text_edit') and self.text_edit:
                    try:
                        # Update font from settings
                        self.text_edit.update_font_from_settings()
                    except Exception as e:
                        print(f"Error updating font from settings: {e}")

                # Update font size label in status bar if font size changed
                if 'font_size' in settings:
                    if hasattr(self, 'font_size_label') and self.font_size_label:
                        self.font_size_label.setText(f"Font Size: {self.settings_manager.font_size} (Ctrl+↑/↓)")

            # Check if auto-save settings changed
            if 'auto_save_enabled' in settings or 'auto_save_interval' in settings:
                # Reconfigure the auto-save timer
                self._setup_auto_save_timer()

            # Check if line numbers setting changed
            if 'show_line_numbers' in settings:
                # Update the visibility of line numbers in the current text editor if it exists
                if hasattr(self, 'text_edit') and self.text_edit:
                    try:
                        # Update line numbers visibility from settings
                        self.text_edit.update_line_numbers_visibility()
                    except Exception as e:
                        print(f"Error updating line numbers visibility from settings: {e}")

            # Check if highlight current line setting changed
            if 'highlight_current_line' in settings:
                # Update the current line highlighting in the text editor if it exists
                if hasattr(self, 'text_edit') and self.text_edit:
                    try:
                        # Call the method to update the highlighting setting
                        self.text_edit.update_highlight_current_line_setting()
                    except Exception as e:
                        print(f"Error updating line highlighting from settings: {e}")

        dialog = SettingsDialog(self.settings_manager, self)
        dialog.settings_changed.connect(on_settings_changed)
        dialog.exec_()

    def update_highlighter_colors(self, styles):
        """Update syntax highlighter colors in the current editor and all open tabs"""
        # Обновляем подсветку синтаксиса, если редактор существует
        if hasattr(self, 'text_edit') and self.text_edit:
            if hasattr(self, 'highlighter') and self.highlighter:
                # Обновляем цвета подсветчика с использованием стилей из темы
                highlighter_colors = {
                    'key_color': styles.get('SyntaxKeyColor', '#E06C75'),
                    'string_color': styles.get('SyntaxStringColor', '#ABB2BF'),
                    'comment_color': styles.get('SyntaxCommentColor', '#608B4E'),
                    'keyword_color': styles.get('SyntaxKeywordColor', '#AF55C4'),
                    'default_color': styles.get('SyntaxDefaultColor', '#CCCCCC')
                }
                self.highlighter.update_colors(highlighter_colors)

                # Force re-highlighting of the entire document to apply new colors
                doc = self.text_edit.document()
                self.highlighter.setDocument(None)
                self.highlighter.setDocument(doc)

        # Обновить также цвета для нумерации строк
        if hasattr(self, 'text_edit') and self.text_edit:
            self.text_edit.styles = {'DarkTheme': styles}
            # Обновить стили панели нумерации строк
            if hasattr(self.text_edit, 'update_line_number_styles'):
                self.text_edit.update_line_number_styles()
            # Перерисовать панель нумерации строк
            self.text_edit.line_numbers.update()

    def change_font_size(self, change):
        from views.shortcuts import change_font_size as _resize
        return _resize(self, change)

    def show_text_edit_context_menu(self, pos):
        menu = QMenu(self)

        # Add animation
        effect = QGraphicsOpacityEffect(menu)
        effect.setOpacity(0)
        menu.setGraphicsEffect(effect)

        animation = QPropertyAnimation(effect, b"opacity")
        animation.setDuration(150)  # milliseconds
        animation.setStartValue(0.0)
        animation.setEndValue(1.0)
        animation.setEasingCurve(QEasingCurve.InQuad)
        animation.start(QPropertyAnimation.DeleteWhenStopped)

        cut_action = QAction("Cut", self)
        cut_action.triggered.connect(self.text_edit.cut)
        menu.addAction(cut_action)

        copy_action = QAction("Copy", self)
        copy_action.triggered.connect(self.text_edit.copy)
        menu.addAction(copy_action)

        paste_action = QAction("Paste", self)
        paste_action.triggered.connect(self.text_edit.paste)
        menu.addAction(paste_action)

        menu.addSeparator()

        select_all_action = QAction("Select All", self)
        select_all_action.triggered.connect(self.text_edit.selectAll)
        menu.addAction(select_all_action)

        menu.exec_(self.text_edit.mapToGlobal(pos))