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
from PyQt5.QtCore import QSize
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
from language_service import LanguageService # Import the LanguageService from the new file
from views.tabs import question_message_box # Import the custom message box function

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

    def _update_svg_colors(self, svg_content: str, folder_color: str = None, yaml_color: str = None) -> str:
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

        # Apply color updates based on loaded styles
        folder_svg_content = self._update_svg_colors(folder_svg_content, folder_color=folder_icon_color)
        yaml_svg_content = self._update_svg_colors(yaml_svg_content, yaml_color=yaml_icon_color)

        # Create icons with applied colors
        self.icon_folder = self._create_icon_from_svg_content(folder_svg_content)
        self.icon_yaml = self._create_icon_from_svg_content(yaml_svg_content)
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

        self.init_ui()
        self.update_status_bar()

        self.session_manager = SessionManager(self)
        self.session_manager.restore_session() # Restore session on startup

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
        event.accept()

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
                self.text_edit.setText("")
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
        self.font_size_label = QLabel(f"Font Size: {self._current_font_size} (Ctrl+↑/↓)")
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
        # ... (reload_structure_action code) ...
        root_path = self.temp_structure.get('root_path')
        if root_path:
            original_path = os.path.normpath(root_path)
            self.reload_language_structure(original_path)
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

            # Обновляем цвета в SVG содержимом
            folder_svg_content = self._update_svg_colors(folder_svg_content, folder_color=folder_icon_color)
            yaml_svg_content = self._update_svg_colors(yaml_svg_content, yaml_color=yaml_icon_color)

            # Пересоздаем иконки с новыми цветами
            self.icon_folder = self._create_icon_from_svg_content(folder_svg_content)
            self.icon_yaml = self._create_icon_from_svg_content(yaml_svg_content)

            # Обновляем подсветку синтаксиса для текущего текстового редактора
            self.update_highlighter_colors(styles)

            # Перерисовываем дерево файлов, чтобы обновить иконки
            if hasattr(self, 'draw_file_tree'):
                self.draw_file_tree()

        dialog = StylesEditorDialog(self, self._get_resource_path('styles.yaml'))
        dialog.styles_changed.connect(on_styles_changed)
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