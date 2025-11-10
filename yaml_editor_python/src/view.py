# src/view.py
import os
import sys
import yaml 
import collections
from typing import Dict, Any, Optional
from PyQt5.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QSplitter,
    QTextEdit, QToolBar, QLabel, QAction, QStatusBar,
    QPushButton, QLineEdit, QSizePolicy, QFileDialog, QScrollArea, QMessageBox
)
from PyQt5.QtGui import QIcon, QFont, QColor
from PyQt5.QtCore import Qt, QSize, QTimer, QCoreApplication, QByteArray, QUrl
# Импорты .models и .icons должны быть доступны, если они в той же папке src
from models import YamlTab, LanguageService
from highlighter import YamlHighlighter
from validator import StructureValidator
from icons import SVG_FOLDER_ICON, SVG_YAML_FILE_ICON

# --- УТИЛИТА ДЛЯ СОЗДАНИЯ QIcon ИЗ SVG-строки ---
def create_icon_from_svg(svg_content: str, size: QSize = QSize(16, 16)) -> QIcon:
    """Создает QIcon из SVG-кода с использованием data URI."""
    svg_bytes = QByteArray(svg_content.encode('utf-8'))
    base64_data = svg_bytes.toBase64().data().decode() 
    data_uri = f'data:image:svg+xml;base64,{base64_data}'
    icon = QIcon(data_uri)
    return icon
# -------------------------------------------------------------------

# --- Класс LanguageService (Оставлен для полноты и корректного сканирования) ---
# ... (Код LanguageService) ...
class LanguageService:
    def normalize_path(self, path: str) -> str:
        if not path: return ""
        return os.path.normpath(path).lower()

    def add_folder_recursive(self, folder_path: str, structure: dict):
        normalized_path = self.normalize_path(folder_path)
        yaml_files = []
        try:
            for file_name in os.listdir(folder_path):
                full_path = os.path.join(folder_path, file_name)
                # Ищем только .yaml файлы, игнорируем .yaml.meta
                if os.path.isfile(full_path) and file_name.lower().endswith('.yaml') and not file_name.lower().endswith('.yaml.meta'):
                    yaml_files.append(file_name)
        except OSError:
            pass
            
        structure[normalized_path] = sorted(yaml_files)

        try:
            for item_name in os.listdir(folder_path):
                full_path = os.path.join(folder_path, item_name)
                if os.path.isdir(full_path):
                    self.add_folder_recursive(full_path, structure)
        except OSError:
            pass

    def get_language_structure_from_path(self, folder_path: str) -> dict:
        normalized_root = self.normalize_path(folder_path)
        structure = {}
        if os.path.isdir(folder_path):
            self.add_folder_recursive(folder_path, structure)
        return {'root_path': normalized_root, 'structure': structure}
# -------------------------------------------------------------------


class YAMLEditorWindow(QMainWindow):
    
    def __init__(self):
        super().__init__()
        
        # --- Инициализация Стилей ---
        self.STYLES = self._load_styles()
        self.CSS_STYLES = self._generate_css(self.STYLES)
        
        self.setWindowTitle("YAML Editor")
        self.setGeometry(100, 100, 1200, 800)
        self.setStyleSheet(self.CSS_STYLES) # Применяем сгенерированные стили
        
        # --- Инициализация Иконок ---
        self.icon_folder = create_icon_from_svg(SVG_FOLDER_ICON)
        self.icon_yaml = create_icon_from_svg(SVG_YAML_FILE_ICON)
        # self.icon_close = create_icon_from_svg(SVG_CLOSE_ICON) # Игнорируем SVG крестик
        
        # --- Модель/Сервисы ---
        self.lang_service = LanguageService() 
        self.validator = StructureValidator() # <-- ИНИЦИАЛИЗАЦИЯ ВАЛИДАТОРА
        self.temp_structure = {'root_path': None, 'structure': {}} 
        self.open_tabs: list[YamlTab] = []
        self.current_tab_index = -1
        self.current_tab: YamlTab | None = None
        
        # UI-переменные (как в C#)
        self.root_lang_path_normalized: str | None = None
        self._notification_timer = QTimer(self)
        self._notification_timer.timeout.connect(self.hide_notification)
        self._notification_label: QLabel | None = None
        self._current_font_size = 14
        self._foldouts: Dict[str, bool] = {} # Для хранения состояния папок
        
        self.init_ui()
        self.update_status_bar()

    def _get_resource_path(self, relative_path: str) -> str:
        """
        Получает путь к ресурсу. 
        В режиме EXE файл находится рядом с исполняемым файлом.
        """
        if getattr(sys, 'frozen', False):
            # В режиме PyInstaller (EXE): файл styles.yaml находится в папке dist
            # рядом с YAML_Editor.exe. sys.executable - это путь к EXE.
            base_path = os.path.dirname(sys.executable)
        else:
            # В режиме разработки: файл styles.yaml находится рядом с view.py (в папке src)
            base_path = os.path.dirname(os.path.abspath(__file__))
        
        # Соединяем базовый путь и имя файла.
        return os.path.join(base_path, relative_path)

    def _load_styles(self) -> Dict[str, Any]:
        """Загружает стили из styles.yaml или использует резервные."""
        
        # КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Используем _get_resource_path для доступа к файлу
        # Поскольку в PyInstaller мы указали --add-data "src/styles.yaml;src",
        # файл будет доступен в папке 'src' относительно базового пути PyInstaller.
        # Но поскольку view.py сам находится в 'src' в режиме разработки, 
        # нам нужно просто обращаться к файлу 'styles.yaml' в нашей папке.
        
        # Если view.py находится в src/, и styles.yaml рядом:
        
        # СТАРЫЙ КОД:
        # style_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'styles.yaml')
        
        # НОВЫЙ КОД (Использует путь, который работает в PyInstaller и в разработке)
        style_file = self._get_resource_path('styles.yaml')

        # --- РЕЗЕРВНЫЕ СТИЛИ (Оставлены без изменений) ---
        default_styles = {
            # ... (Ваши стили) ...
            'DarkTheme': {
                'Background': "#3C3C3C", 'Foreground': "#CCCCCC", 'SecondaryBackground': "#4C4C4C",
                'EditorBackground': "#2D2D2D", 'BorderColor': "#1D1D1D", 'HighlightColor': "#0078D7", 
                'HoverColor': "#5C5C5C", 'FilePanelBackground': "#333333", 'FilePanelHover': "#4C4C4C", 
                'FolderColor': "#FFC107", 'StatusDefault': "#AAAAAA", 'NotificationSuccess': "#28A745", 
                'NotificationError': "#DC3545",
                'NotificationWarning': "#FFC107" 
            }
        }
        
        # ... (Остальная логика загрузки файла) ...
        try:
            if os.path.exists(style_file):
                 # ... (Остальная логика загрузки) ...
                with open(style_file, 'r', encoding='utf-8') as f:
                    styles = yaml.safe_load(f)
                    # ... (Проверка ключей и возврат) ...
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

    # ... (Остальная часть _generate_css) ...
    def _generate_css(self, styles: Dict[str, Any]) -> str:
        """Генерирует CSS-строку на основе загруженных стилей."""
        theme = styles.get('DarkTheme', {})
        
        css = f"""
        QMainWindow, QWidget {{ background-color: {theme.get('Background')}; color: {theme.get('Foreground')}; }}
        QSplitter::handle {{ background-color: {theme.get('Background')}; }}
        
        /* TULBAR & ACTIONS (Исправлены визуальные ошибки кнопок QAction) */
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
            border: 1px solid {theme.get('BorderColor')}; 
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
        """
        return css

    def init_ui(self):
        # ... (Код init_ui) ...
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
        
    def create_main_toolbar(self):
        # ... (Код create_main_toolbar) ...
        main_toolbar = QToolBar("Main Toolbar")
        main_toolbar.setIconSize(QSize(18, 18))
        self.addToolBar(Qt.TopToolBarArea, main_toolbar)
        
        main_toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        # --- КНОПКА OPEN FOLDER ---
        open_action = QAction(self.icon_folder, "Open Folder...", self)
        open_action.triggered.connect(self.open_folder_dialog)
        main_toolbar.addAction(open_action)
        
        main_toolbar.addSeparator()
        
        # Reload Structure
        reload_action = QAction("Reload Structure", self)
        reload_action.triggered.connect(self.reload_structure_action)
        main_toolbar.addAction(reload_action)
        
        main_toolbar.addSeparator()
        
        # Заглушка для выбора языка
        self.language_label = QLabel("Language: N/A")
        main_toolbar.addWidget(self.language_label)
        
        # Заглушка для Font Size
        self.font_size_label = QLabel(f"Font Size: {self._current_font_size} (Ctrl+↑/↓)")
        main_toolbar.addWidget(self.font_size_label)
        
        # Гибкое пространство
        main_toolbar.addSeparator()
        spacer = QWidget()
        spacer.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
        main_toolbar.addWidget(spacer)

    def open_folder_dialog(self):
        """Открывает диалог для выбора корневой папки локализации."""
        initial_dir = os.path.expanduser("~") 
        folder_path = QFileDialog.getExistingDirectory(self, "Select Language Root Folder", initial_dir)
        
        if folder_path:
            self.reload_language_structure(folder_path)

    
    def reload_language_structure(self, folder_path: str):
        """Перезагружает структуру файлов из выбранной папки."""
        
        dirty_tab = next((t for t in self.open_tabs if t.is_dirty), None)

        if dirty_tab:
            reply = QMessageBox.question(self, 'Unsaved Changes', 
                f"Do you want to save changes to file '{os.path.basename(dirty_tab.file_path)}' before reloading the structure? Changes will be lost for other unsaved files.",
                QMessageBox.Save | QMessageBox.Discard | QMessageBox.Cancel, QMessageBox.Cancel)
                
            if reply == QMessageBox.Save:
                for tab in [t for t in self.open_tabs if t.is_dirty]:
                    self.save_file_action(tab)
            elif reply == QMessageBox.Cancel:
                return 
        
        # Сбрасываем открытые вкладки
        self.open_tabs.clear()
        self.current_tab_index = -1
        self.current_tab = None
        
        # Загружаем новую структуру
        new_structure = self.lang_service.get_language_structure_from_path(folder_path)
        
        # --- ПРОВЕРКА ВАЛИДАЦИИ ---
        if not self.validator.validate_structure(new_structure):
            # Если невалидно, сбрасываем и показываем ошибку
            self.temp_structure = {'root_path': None, 'structure': {}}
            self.root_lang_path_normalized = None
            self.language_label.setText(f"Language: N/A (Invalid)")
            self.draw_file_tree()
            self.draw_tabs_placeholder()
            self.update_status_bar()
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            error_msg = self.validator.get_last_error()
            self.show_notification(f"Validation failed for {os.path.basename(folder_path)}: {error_msg}", color, duration_ms=5000)
            return

        # Если валидно
        self.temp_structure = new_structure
        self.root_lang_path_normalized = self.temp_structure.get('root_path')
        
        if self.root_lang_path_normalized and self.temp_structure.get('structure'):
            self.language_label.setText(f"Language: {os.path.basename(folder_path)}")
            self.draw_file_tree() 
            self.draw_tabs_placeholder()
            self.update_status_bar()
            color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
            self.show_notification(f"Structure loaded and validated from: {os.path.basename(folder_path)}", color)
        else:
            self.language_label.setText(f"Language: N/A")
            self.draw_file_tree()
            self.draw_tabs_placeholder()
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification("Failed to load structure. Folder not found or empty.", color)

    # --- Валидация ---
    def validate_structure(self):
        """Использует StructureValidator для проверки текущей структуры."""
        is_valid = self.validator.validate_structure(self.temp_structure)
        if is_valid:
            self.status_bar.showMessage("Structure validation passed.", 3000)
        else:
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification(f"Structure validation failed: {self.validator.get_last_error()}", color, duration_ms=5000)
            
    # ... (Остальная часть validate_yaml) ...
    def validate_yaml(self, file_path: str, yaml_text: str) -> bool:
        try:
            yaml.safe_load(yaml_text)
            return True
        except yaml.YAMLError as e:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification(f"YAML Syntax Error in {os.path.basename(file_path)}!", color)
            print(f"YAML Error: {e}")
            return False 

    # ... (Остальная часть File Panel: create_file_panel, clear_layout, draw_file_tree, foldout methods) ...
    def create_file_panel(self) -> QWidget:
        panel = QWidget()
        panel.setObjectName("FilePanelWidget") # Для применения стиля CSS
        layout = QVBoxLayout(panel)
        layout.setContentsMargins(5, 5, 5, 5)

        search_bar = QWidget()
        search_layout = QHBoxLayout(search_bar)
        search_layout.setContentsMargins(0, 0, 0, 0)
        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("Search files...")
        self.search_input.setStyleSheet(f"background-color: {self.STYLES['DarkTheme']['HoverColor']}; color: white; border-radius: 5px;")
        self.search_input.textChanged.connect(self.draw_file_tree)
        search_layout.addWidget(self.search_input)
        
        clear_search_btn = QPushButton("x")
        clear_search_btn.setFixedSize(20, 20)
        clear_search_btn.setStyleSheet("font-weight: bold; padding: 0;")
        clear_search_btn.clicked.connect(lambda: self.search_input.setText(""))
        search_layout.addWidget(clear_search_btn)
        
        layout.addWidget(search_bar)

        self.file_tree_content = QWidget()
        self.file_tree_layout = QVBoxLayout(self.file_tree_content)
        self.file_tree_layout.setContentsMargins(0, 0, 0, 0)
        self.file_tree_layout.setSpacing(1)
        self.file_tree_content.setSizePolicy(QSizePolicy.Minimum, QSizePolicy.Fixed)

        self.file_scroll_area = QScrollArea()
        self.file_scroll_area.setWidgetResizable(True)
        self.file_scroll_area.setWidget(self.file_tree_content)
        
        layout.addWidget(self.file_scroll_area, 1)
        
        self.draw_file_tree()
        
        return panel

    def clear_layout(self, layout):
        if layout is not None:
            while layout.count():
                item = layout.takeAt(0)
                widget = item.widget()
                if widget is not None:
                    widget.deleteLater()
                else:
                    self.clear_layout(item.layout())

    def draw_file_tree(self):
        self.clear_layout(self.file_tree_layout)
        
        root_path = self.temp_structure.get('root_path')
        structure = self.temp_structure.get('structure', {})

        if not root_path or not structure:
            placeholder = QLabel("Select language folder via 'Open Folder...'")
            placeholder.setAlignment(Qt.AlignCenter)
            self.file_tree_layout.addWidget(placeholder)
            self.file_tree_layout.addStretch(1)
            return
        
        self.draw_folder_content(root_path, structure, 0)
        self.file_tree_layout.addStretch(1)

    def get_or_set_foldout(self, path: str, default: bool = False) -> bool:
        normalized = self.lang_service.normalize_path(path)
        if normalized not in self._foldouts:
            self._foldouts[normalized] = default
        return self._foldouts[normalized]

    def set_foldout(self, path: str, state: bool):
        normalized = self.lang_service.normalize_path(path)
        self._foldouts[normalized] = state
        self.draw_file_tree() 

    def check_folder_for_match_recursive(self, folder_path_normalized: str) -> bool:
        query = self.search_input.text().lower()
        if not query: return True
        if folder_path_normalized not in self.temp_structure['structure']: return False

        if any(f.lower().startswith(query) for f in self.temp_structure['structure'][folder_path_normalized]):
            return True

        path_prefix = folder_path_normalized + os.sep
        sub_folders = [k for k in self.temp_structure['structure'].keys() 
                       if k.startswith(path_prefix) and 
                       self.lang_service.normalize_path(os.path.dirname(k)) == folder_path_normalized] 
                       
        for sub_folder_path in sorted(sub_folders):
            if self.check_folder_for_match_recursive(sub_folder_path):
                return True
        
        return False
        
    def draw_folder_content(self, folder_path: str, structure: dict, level: int):
        normalized_path = self.lang_service.normalize_path(folder_path)
        if normalized_path not in structure:
            return

        is_searching = bool(self.search_input.text())
        folder_matches = self.check_folder_for_match_recursive(normalized_path)
        
        if is_searching and not folder_matches and normalized_path != self.root_lang_path_normalized:
            return

        folder_name = os.path.basename(folder_path)
        should_be_open = True 
        
        if normalized_path == self.root_lang_path_normalized:
            folder_name = os.path.basename(folder_path) 
            should_be_open = self.get_or_set_foldout(normalized_path, default=True) 
            
        elif is_searching and folder_matches:
            should_be_open = True 
        else:
            should_be_open = self.get_or_set_foldout(normalized_path) 
        
        
        # 1. Отрисовка текущей папки/кнопки-переключателя
        folder_color = self.STYLES['DarkTheme']['FolderColor']
        
        if normalized_path == self.root_lang_path_normalized:
            label = QLabel(f" {folder_name}")
            label.setPixmap(self.icon_folder.pixmap(QSize(16, 16)))
            label.setStyleSheet(f"color: {folder_color}; padding: 2px 0; margin-left: 0px; font-weight: bold;")
            self.file_tree_layout.addWidget(label)
        
        else:
            icon_char = '▼' if should_be_open and not (is_searching and folder_matches) else '►'
            
            folder_button = QPushButton(f" {icon_char} {folder_name}")
            folder_button.setFlat(True)
            folder_button.setIcon(self.icon_folder)
            
            # Стиль для папки
            folder_button.setStyleSheet(f"""
                QPushButton {{ 
                    text-align: left; 
                    border: none; 
                    padding: 2px 0;
                    padding-left: {(level) * 15}px;
                    color: {folder_color};
                }}
                QPushButton:hover {{
                    background-color: {self.STYLES['DarkTheme']['FilePanelHover']};
                }}
            """)
            
            if not is_searching or not folder_matches:
                folder_button.clicked.connect(lambda _, path=normalized_path, state=should_be_open: self.set_foldout(path, not state))
            
            self.file_tree_layout.addWidget(folder_button)

        # 2. Отрисовка файлов и рекурсивный вызов, если should_be_open
        if should_be_open or is_searching:
            files = sorted(structure.get(normalized_path, []))
            
            query = self.search_input.text().lower()
            filtered_files = [f for f in files if not is_searching or f.lower().startswith(query)]
            
            for file in filtered_files:
                full_path = os.path.join(folder_path, file)
                self._add_file_button(file, full_path, level)

            path_prefix = normalized_path + os.sep
            sub_folders = [k for k in structure.keys() 
                           if k.startswith(path_prefix) and 
                           self.lang_service.normalize_path(os.path.dirname(k)) == normalized_path] 
                           
            for sub_folder_path in sorted(sub_folders):
                self.draw_folder_content(sub_folder_path, structure, level + 1)
            
    
    def _add_file_button(self, name: str, path: str, level: int):
        normalized_path = self.lang_service.normalize_path(path)
        is_selected = self.current_tab and self.lang_service.normalize_path(self.current_tab.file_path) == normalized_path
        
        display_name = name
        
        open_tab = next((t for t in self.open_tabs if self.lang_service.normalize_path(t.file_path) == normalized_path), None)
        if (open_tab and open_tab.is_dirty and not is_selected) or (is_selected and self.current_tab.is_dirty):
            display_name += "*"

        file_button = QPushButton(display_name)
        
        if name.lower().endswith(".yaml"):
            file_button.setIcon(self.icon_yaml)
        
        highlight_color = self.STYLES['DarkTheme']['HighlightColor']
        hover_color = self.STYLES['DarkTheme']['FilePanelHover']
        
        base_style = f"text-align: left; border: none; padding: 2px 0; padding-left: {(level + 1) * 15}px;"
        
        if is_selected:
            style = f"""
                QPushButton {{ 
                    {base_style} 
                    background-color: {highlight_color}; 
                    color: white;
                }}
            """
        else:
            style = f"""
                QPushButton {{ 
                    {base_style} 
                    color: {self.STYLES['DarkTheme']['Foreground']};
                }}
                QPushButton:hover {{
                    background-color: {hover_color};
                }}
            """
            
        file_button.setStyleSheet(style)
        file_button.clicked.connect(lambda: self.try_switch_file_action(path))
        self.file_tree_layout.addWidget(file_button)
        
    def load_file(self, file_path: str):
        # ... (Код load_file) ...
        if not file_path: return

        normalized_path = self.lang_service.normalize_path(file_path)
        existing_tab = next((t for t in self.open_tabs if self.lang_service.normalize_path(t.file_path) == normalized_path), None)

        if existing_tab:
            self.switch_tab_action(self.open_tabs.index(existing_tab))
            return

        file_content = ""
        try:
            if os.path.exists(file_path):
                with open(file_path, 'r', encoding='utf-8') as f:
                    file_content = f.read()
            else:
                color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
                self.show_notification(f"File not found: {os.path.basename(file_path)}", color)
                return
        except Exception as e:
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification(f"Error reading file: {os.path.basename(file_path)}", color)
            print(f"File read error: {e}")
            return
            
        new_tab = YamlTab(file_path, file_content)
        self.open_tabs.append(new_tab)
        self.current_tab_index = len(self.open_tabs) - 1
        self.current_tab = new_tab
        
        self.validate_yaml(file_path, file_content) 
        self.text_edit.setText(self.current_tab.yaml_text)
        self.draw_tabs_placeholder()
        self.draw_file_tree()
        self.update_status_bar()
        self.update_undo_redo_ui()

    def try_switch_file_action(self, new_file_path: str):
        # ... (Код try_switch_file_action) ...
        file_name = os.path.basename(new_file_path).lower()
        if file_name.endswith('.meta') or file_name.endswith('.png'):
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification(f"Cannot open {file_name}.", color)
            return
            
        current_tab = self.current_tab
        new_path_normalized = self.lang_service.normalize_path(new_file_path)
        
        if current_tab and self.lang_service.normalize_path(current_tab.file_path) == new_path_normalized:
            return

        if current_tab is None or not current_tab.is_dirty:
            self.load_file(new_file_path)
            return

        current_file_name = os.path.basename(current_tab.file_path)
        
        reply = QMessageBox.question(self, 'Unsaved Changes',
            f"Do you want to save changes to file '{current_file_name}' before switching to '{os.path.basename(new_file_path)}'?",
            QMessageBox.Save | QMessageBox.Discard | QMessageBox.Cancel, QMessageBox.Cancel)

        if reply == QMessageBox.Save:
            self.save_file_action(current_tab)
            if not current_tab.is_dirty: 
                 self.load_file(new_file_path)
        elif reply == QMessageBox.Discard:
            current_tab.is_dirty = False 
            self.load_file(new_file_path)
        
        self.draw_tabs_placeholder()
        self.draw_file_tree()
        self.update_status_bar()


    def create_editor_area(self) -> QWidget:
        # ... (Код create_editor_area) ...
        area = QWidget()
        layout = QVBoxLayout(area)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        
        self.tab_placeholder = QToolBar("TabPlaceholder")
        self.tab_placeholder.setFixedHeight(30) 
        self.draw_tabs_placeholder()
        layout.addWidget(self.tab_placeholder)

        editor_widget = QWidget()
        editor_layout = QVBoxLayout(editor_widget)
        editor_layout.setContentsMargins(5, 5, 5, 5)
        editor_layout.setSpacing(5)

        self.editor_toolbar = QToolBar("Editor Toolbar")
        self.editor_toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)
        self.create_editor_toolbar()
        editor_layout.addWidget(self.editor_toolbar)

        self.text_edit = QTextEdit()
        self.text_edit.setFont(QFont("Consolas", self._current_font_size))
        self.text_edit.setText(self.current_tab.yaml_text if self.current_tab else "") 
        self.text_edit.textChanged.connect(self.handle_text_change)
        
        self.highlighter = YamlHighlighter(self.text_edit.document()) 
        
        editor_layout.addWidget(self.text_edit)
        
        layout.addWidget(editor_widget)
        
        return area

    def create_editor_toolbar(self):
        # ... (Код create_editor_toolbar) ...
        # Reload
        reload_action = QAction("Reload", self)
        reload_action.triggered.connect(self.reload_file_action)
        self.editor_toolbar.addAction(reload_action)
        
        # Save
        save_action = QAction("Save (Ctrl+S)", self)
        save_action.setShortcut("Ctrl+S")
        save_action.triggered.connect(lambda: self.save_file_action(self.current_tab))
        self.editor_toolbar.addAction(save_action)
        
        self.editor_toolbar.addSeparator()

        # Undo
        self.undo_action = QAction("Undo (Ctrl+Z)", self)
        self.undo_action.setShortcut("Ctrl+Z")
        self.undo_action.triggered.connect(self.handle_undo)
        self.editor_toolbar.addAction(self.undo_action)
        
        # Redo
        self.redo_action = QAction("Redo (Ctrl+Y)", self)
        self.redo_action.setShortcut("Ctrl+Y")
        self.redo_action.triggered.connect(self.handle_redo)
        self.editor_toolbar.addAction(self.redo_action)
        
        spacer = QWidget()
        spacer.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
        self.editor_toolbar.addWidget(spacer)


    def create_status_bar(self):
        # ... (Код create_status_bar) ...
        self.status_bar = QStatusBar()
        self.setStatusBar(self.status_bar)
        self.status_label = QLabel("Ready.")
        self.status_bar.addWidget(self.status_label)
        
        self._notification_label = QLabel()
        self._notification_label.setVisible(False)
        self.status_bar.addPermanentWidget(self._notification_label)
        
    def show_notification(self, message: str, color: QColor, duration_ms: int = 3000):
        # ... (Код show_notification) ...
        if self._notification_label:
            self._notification_label.setText(message)
            # Используем QColor.name() для получения строки #RRGGBB
            color_hex = color.name() 
            self._notification_label.setStyleSheet(f"background-color: {color_hex}; padding: 0 10px; color: black; border-radius: 3px;")
            self._notification_label.setVisible(True)
            self._notification_timer.start(duration_ms)

    def hide_notification(self):
        # ... (Код hide_notification) ...
        if self._notification_label:
            self._notification_label.setVisible(False)
            self._notification_timer.stop()


    def draw_tabs_placeholder(self):
        # ... (Код draw_tabs_placeholder с крестиком Юникода) ...
        self.tab_placeholder.clear()
        if not self.open_tabs: return

        highlight_color = self.STYLES['DarkTheme']['HighlightColor']
        bg_color = self.STYLES['DarkTheme']['Background']
        editor_bg = self.STYLES['DarkTheme']['EditorBackground']
        hover_bg = self.STYLES['DarkTheme']['FilePanelHover']
        
        close_icon_color = "#FFFFFF" # Гарантированно белый цвет

        for i, tab in enumerate(self.open_tabs):
            tab_name = os.path.basename(tab.file_path)
            if tab.is_dirty: tab_name += "*"
                
            tab_container = QWidget()
            tab_layout = QHBoxLayout(tab_container)
            tab_layout.setContentsMargins(5, 0, 2, 0)
            tab_layout.setSpacing(4)
            
            tab_label = QLabel(tab_name)
            tab_layout.addWidget(tab_label)

            # --- КНОПКА ЗАКРЫТИЯ (КРЕСТИК ЮНИКОДА) ---
            close_button = QPushButton("×") 
            close_button.setFixedSize(QSize(20, 20))
            
            close_button.setStyleSheet(f"""
                QPushButton {{
                    background: transparent; 
                    border: none; 
                    margin: 0; 
                    padding: 0; 
                    color: {close_icon_color}; 
                    font-weight: bold; 
                    font-size: 14px; 
                }}
                QPushButton:hover {{
                    background: transparent;
                }}
            """)
            
            close_button.setCursor(Qt.PointingHandCursor)
            
            close_button.clicked.connect(lambda checked, idx=i: self.try_close_tab(idx)) 
            tab_layout.addWidget(close_button)

            # Переключение вкладки при клике
            tab_container.mousePressEvent = lambda event, index=i: self.switch_tab_action(index)
            tab_container.setCursor(Qt.PointingHandCursor)
            
            is_active = i == self.current_tab_index
            style_sheet = f"""
                QWidget {{ padding: 4px 10px; margin: 0 1px; border-bottom: 2px solid transparent; background-color: {bg_color};}}
            """
            if is_active:
                style_sheet += f"""
                    QWidget {{ background-color: {editor_bg}; color: white; border-bottom: 2px solid {highlight_color}; }}
                """
            else:
                style_sheet += f"""
                    QWidget:hover {{ background-color: {hover_bg}; }}
                """
                
            tab_container.setStyleSheet(style_sheet)
            
            self.tab_placeholder.addWidget(tab_container)
            
        spacer = QWidget()
        spacer.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
        self.tab_placeholder.addWidget(spacer)


    def switch_tab_action(self, index):
        # ... (Код switch_tab_action) ...
        if index == self.current_tab_index: return
            
        self.current_tab_index = index
        self.current_tab = self.open_tabs[index]
        self.text_edit.setText(self.current_tab.yaml_text)
        
        self.draw_tabs_placeholder()
        self.draw_file_tree() 
        self.update_status_bar()
        self.update_undo_redo_ui()

    def handle_text_change(self):
        # ... (Код handle_text_change) ...
        if not self.current_tab: return
            
        new_text = self.text_edit.toPlainText()
        
        if self.current_tab.yaml_text != new_text:
             if len(self.current_tab.undo_stack) > 0 and self.current_tab.undo_stack[-1] != self.current_tab.yaml_text:
                 self.current_tab.undo_stack.append(self.current_tab.yaml_text)
             elif len(self.current_tab.undo_stack) == 0:
                 self.current_tab.undo_stack.append(self.current_tab.yaml_text)
                 
             self.current_tab.redo_stack.clear()
             self.current_tab.yaml_text = new_text
             self.current_tab.is_dirty = True
             self.validate_yaml(self.current_tab.file_path, new_text) 
            
        self.draw_tabs_placeholder() 
        self.draw_file_tree() 
        self.update_status_bar()
        self.update_undo_redo_ui()
        

    def try_close_tab(self, index: int):
        """
        Обрабатывает запрос на закрытие вкладки по индексу, 
        проверяя несохраненные изменения и обновляя содержимое QTextEdit.
        
        ИСПРАВЛЕНИЕ: Гарантирует, что после удаления вкладки поле ввода
        обновится содержимым новой активной вкладки или очистится.
        """
        
        if not (0 <= index < len(self.open_tabs)):
            return 
            
        tab_to_close = self.open_tabs[index]

        # --- 1. Проверка сохранения ---
        if tab_to_close.is_dirty:
            reply = QMessageBox.question(self, 'Сохранить изменения', 
                                         f"Файл '{os.path.basename(tab_to_close.file_path)}' имеет несохраненные изменения. Хотите сохранить?",
                                         QMessageBox.Save | QMessageBox.Discard | QMessageBox.Cancel, QMessageBox.Cancel)
            
            if reply == QMessageBox.Cancel:
                return # Отмена закрытия
            
            if reply == QMessageBox.Save:
                # Предполагаем, что save_file_action обновляет tab_to_save.is_dirty
                self.save_file_action(tab_to_close) 
                if tab_to_close.is_dirty: 
                    return # Если сохранить не удалось, отменяем закрытие

        # --- 2. Удаление вкладки из списка моделей (YAMLTab) ---
        self.open_tabs.pop(index)

        # --- 3. Обновление текущей активной вкладки и контента редактора ---
        if self.open_tabs:
            # 1. Определяем новый индекс
            # Если удаленная вкладка была последней, переходим к предпоследней, иначе - к вкладке на ее месте.
            new_index = min(index, len(self.open_tabs) - 1)
            
            # 2. Устанавливаем новую активную вкладку и обновляем контент
            # Вместо дублирования логики переключения, вызываем существующий метод switch_tab_action.
            # Если switch_tab_action корректно реализован (как показано выше), он сделает все необходимое.
            self.switch_tab_action(new_index)
            
        else:
            # Если вкладок не осталось: очищаем поле ввода
            self.current_tab = None
            self.current_tab_index = -1
            self.text_edit.setText("") # <--- КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: Очистка редактора
            self.text_edit.clearUndoRedoStacks()

        # --- 4. Обновление остальных элементов UI ---
        self.draw_tabs_placeholder() # Обновление списка вкладок в UI
        self.draw_file_tree()
        self.update_status_bar()
        self.update_undo_redo_ui()

    def update_status_bar(self):
        # ... (Код update_status_bar) ...
        status = "Ready."
        if self.current_tab and self.current_tab.is_dirty:
            status = f"Unsaved changes in {os.path.basename(self.current_tab.file_path)}*"
        self.status_label.setText(status)
        
    def update_undo_redo_ui(self):
        # ... (Код update_undo_redo_ui) ...
        if not self.current_tab:
            self.undo_action.setEnabled(False)
            self.redo_action.setEnabled(False)
            return
            
        self.undo_action.setEnabled(len(self.current_tab.undo_stack) > 0)
        self.redo_action.setEnabled(len(self.current_tab.redo_stack) > 0)


    def save_file_action(self, tab_to_save: YamlTab | None):
        # ... (Код save_file_action) ...
        if not tab_to_save or not tab_to_save.is_dirty or not tab_to_save.file_path:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification("Nothing to save.", color)
            return

        try:
            if not self.validate_yaml(tab_to_save.file_path, tab_to_save.yaml_text):
                 color = QColor(self.STYLES['DarkTheme']['NotificationError'])
                 self.show_notification(f"Cannot save: YAML syntax error in {os.path.basename(tab_to_save.file_path)}.", color)
                 return

            with open(tab_to_save.file_path, 'w', encoding='utf-8') as f:
                f.write(tab_to_save.yaml_text)
            
            tab_to_save.is_dirty = False 
            
            self.draw_tabs_placeholder() 
            self.draw_file_tree()
            self.update_status_bar()
            color = QColor(self.STYLES['DarkTheme']['NotificationSuccess'])
            self.show_notification(f"File saved successfully: {os.path.basename(tab_to_save.file_path)}", color)
            
        except Exception as ex:
            color = QColor(self.STYLES['DarkTheme']['NotificationError'])
            self.show_notification(f"Failed to save: {os.path.basename(tab_to_save.file_path)}", color)
            print(f"File save error: {ex}")

    def reload_file_action(self):
        # ... (Код reload_file_action) ...
        if self.current_tab:
             self.load_file(self.current_tab.file_path)
             color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
             self.show_notification(f"File reloaded: {os.path.basename(self.current_tab.file_path)}", color)
        else:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification("No file open to reload.", color)


    def reload_structure_action(self):
        # ... (Код reload_structure_action) ...
        root_path = self.temp_structure.get('root_path')
        if root_path:
            original_path = os.path.normpath(root_path) 
            self.reload_language_structure(original_path)
        else:
            color = QColor(self.STYLES['DarkTheme']['NotificationWarning'])
            self.show_notification("No folder open to reload.", color)

    def handle_undo(self):
        # ... (Код handle_undo) ...
        if not self.current_tab: return

        if len(self.current_tab.undo_stack) > 0:
            self.current_tab.redo_stack.append(self.current_tab.yaml_text)
            text_to_restore = self.current_tab.undo_stack.pop()
            
            self.current_tab._yaml_text = text_to_restore 
            self.current_tab.is_dirty = True
            
            self.text_edit.setText(text_to_restore)
            self.draw_tabs_placeholder()
            self.draw_file_tree()
            self.update_status_bar()
            self.update_undo_redo_ui()
            
    def handle_redo(self):
        # ... (Код handle_redo) ...
        if not self.current_tab: return
        
        if len(self.current_tab.redo_stack) > 0:
            self.current_tab.undo_stack.append(self.current_tab.yaml_text)
            text_to_restore = self.current_tab.redo_stack.pop()

            self.current_tab._yaml_text = text_to_restore 
            self.current_tab.is_dirty = True
            
            self.text_edit.setText(text_to_restore)
            self.draw_tabs_placeholder()
            self.draw_file_tree()
            self.update_status_bar()
            self.update_undo_redo_ui()


    def keyPressEvent(self, event):
        # ... (Код keyPressEvent) ...
        if event.modifiers() == Qt.ControlModifier:
            if event.key() == Qt.Key_Up:
                self.change_font_size(1)
                event.accept()
                return
            if event.key() == Qt.Key_Down:
                self.change_font_size(-1)
                event.accept()
                return
            if event.key() == Qt.Key_S and self.current_tab:
                self.save_file_action(self.current_tab)
                event.accept()
                return
            if event.key() == Qt.Key_Z and self.undo_action.isEnabled():
                self.handle_undo()
                event.accept()
                return
            if event.key() == Qt.Key_Y and self.redo_action.isEnabled():
                self.handle_redo()
                event.accept()
                return
                
        super().keyPressEvent(event)
        
    def change_font_size(self, change):
        # ... (Код change_font_size) ...
        new_size = self._current_font_size + change
        
        if 10 <= new_size <= 24:
            self._current_font_size = new_size
            font = self.text_edit.font()
            font.setPointSize(new_size)
            self.text_edit.setFont(font)
            
            self.font_size_label.setText(f"Font Size: {new_size} (Ctrl+↑/↓)")