from PyQt5.QtWidgets import QToolBar, QLabel, QAction, QWidget, QSizePolicy, QComboBox
from PyQt5.QtCore import Qt, QSize


def create_main_toolbar(self):
    """Create the main toolbar (moved out from view.py)."""
    main_toolbar = QToolBar("Main Toolbar")
    main_toolbar.setIconSize(QSize(18, 18))
    self.addToolBar(Qt.TopToolBarArea, main_toolbar)
    
    main_toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

    # --- OPEN FOLDER BUTTON ---
    open_action = QAction(self.icon_folder, "Open Folder...", self)
    open_action.triggered.connect(self.open_folder_dialog)
    main_toolbar.addAction(open_action)
    
    main_toolbar.addSeparator()
    
    # Reload Structure
    reload_action = QAction("Reload Structure", self)
    reload_action.triggered.connect(self.reload_structure_action)
    main_toolbar.addAction(reload_action)

    main_toolbar.addSeparator()

    # Regenerate Language Manifest
    manifest_action = QAction("Regenerate Manifest", self)
    manifest_action.triggered.connect(self.regenerate_language_manifest)
    main_toolbar.addAction(manifest_action)

    main_toolbar.addSeparator()

    # Create New Language
    new_lang_action = QAction("New Language", self)
    new_lang_action.triggered.connect(self.create_new_language)
    main_toolbar.addAction(new_lang_action)

    main_toolbar.addSeparator()

    # Removed: Flag + label for language selection
    # self.flag_label = QLabel()
    # self.flag_label.setFixedSize(24, 16)
    # self.flag_label.setScaledContents(True)
    # main_toolbar.addWidget(self.flag_label)

    # self.language_label = QLabel("Language: N/A") # Removed
    # main_toolbar.addWidget(self.language_label) # Removed

    # Placeholder for Font Size (Removed)
    # self.font_size_label = QLabel(f"Font Size: {self._current_font_size} (Ctrl+↑/↓)")
    # main_toolbar.addWidget(self.font_size_label)

    # Flexible space
    main_toolbar.addSeparator()
    spacer = QWidget()
    spacer.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
    main_toolbar.addWidget(spacer)

    # Styles Editor Button
    styles_action = QAction("Styles Editor", self)
    styles_action.triggered.connect(self.open_styles_editor)
    main_toolbar.addAction(styles_action)

    return main_toolbar
