"""
Module to create a new language from an existing template language.
"""
import os
import shutil
from typing import Dict, Any
from PyQt5.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, QPushButton, QComboBox, QFileDialog, QMessageBox
from PyQt5.QtCore import pyqtSignal
from PyQt5.QtGui import QColor


class NewLanguageDialog(QDialog):
    """Dialog for creating a new language from a template."""
    
    language_created = pyqtSignal(str, str)  # Emits (new_language_code, new_language_path)
    
    def __init__(self, parent_window, available_languages, root_path):
        super().__init__(parent_window)
        self.parent_window = parent_window
        self.available_languages = available_languages
        self.root_path = root_path
        
        self.setWindowTitle("Create New Language from Template")
        self.setModal(True)
        
        self.setup_ui()
        
    def setup_ui(self):
        layout = QVBoxLayout(self)
        
        # New language code
        code_layout = QHBoxLayout()
        code_layout.addWidget(QLabel("New Language Code:"))
        self.code_input = QLineEdit()
        self.code_input.setPlaceholderText("e.g., es, fr, de...")
        code_layout.addWidget(self.code_input)
        layout.addLayout(code_layout)
        
        # New language display name
        name_layout = QHBoxLayout()
        name_layout.addWidget(QLabel("Language Name:"))
        self.name_input = QLineEdit()
        self.name_input.setPlaceholderText("e.g., Spanish, French, German...")
        name_layout.addWidget(self.name_input)
        layout.addLayout(name_layout)
        
        # Source language template
        template_layout = QHBoxLayout()
        template_layout.addWidget(QLabel("Based on Language:"))
        self.template_combo = QComboBox()
        for lang_code in self.available_languages:
            self.template_combo.addItem(lang_code.upper(), lang_code)
        template_layout.addWidget(self.template_combo)
        layout.addLayout(template_layout)
        
        # Flag selection
        flag_layout = QHBoxLayout()
        flag_layout.addWidget(QLabel("Flag Image:"))
        self.flag_path_input = QLineEdit()
        self.flag_path_input.setPlaceholderText("Select flag image file...")
        self.flag_path_input.setReadOnly(True)
        flag_button = QPushButton("Browse...")
        flag_button.clicked.connect(self.select_flag_image)
        flag_layout.addWidget(self.flag_path_input)
        flag_layout.addWidget(flag_button)
        layout.addLayout(flag_layout)
        
        # Buttons
        button_layout = QHBoxLayout()
        create_button = QPushButton("Create Language")
        create_button.clicked.connect(self.create_language)
        cancel_button = QPushButton("Cancel")
        cancel_button.clicked.connect(self.reject)
        button_layout.addWidget(create_button)
        button_layout.addWidget(cancel_button)
        layout.addLayout(button_layout)
        
        # Set dialog size
        self.resize(400, 150)
    
    def select_flag_image(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, 
            "Select Flag Image", 
            "", 
            "Image Files (*.png *.jpg *.jpeg *.gif *.bmp)"
        )
        if file_path:
            self.flag_path_input.setText(file_path)
    
    def create_language(self):
        new_code = self.code_input.text().strip()
        new_name = self.name_input.text().strip()
        source_code = self.template_combo.currentData()
        flag_path = self.flag_path_input.text().strip()
        
        # Validate inputs
        if not new_code:
            QMessageBox.warning(self, "Invalid Input", "Please enter a language code.")
            return
            
        if not new_name:
            QMessageBox.warning(self, "Invalid Input", "Please enter a language name.")
            return
            
        if not source_code:
            QMessageBox.warning(self, "Invalid Input", "Please select a source language to base on.")
            return
        
        # Check if language code already exists
        new_lang_path = os.path.join(self.root_path, new_code)
        if os.path.exists(new_lang_path):
            reply = QMessageBox.question(
                self, 
                "Language Exists", 
                f"Language '{new_code}' already exists. Do you want to overwrite it?",
                QMessageBox.Yes | QMessageBox.No
            )
            if reply == QMessageBox.No:
                return
        
        # Create the new language
        try:
            success = create_language_from_template(
                self.root_path, 
                source_code, 
                new_code, 
                new_name,
                flag_path if flag_path else None,
                self.parent_window.STYLES if hasattr(self.parent_window, 'STYLES') else {}
            )
            
            if success:
                self.language_created.emit(new_code, new_lang_path)
                self.accept()
            else:
                QMessageBox.critical(self, "Error", "Failed to create the new language.")
        except Exception as e:
            QMessageBox.critical(self, "Error", f"Failed to create the new language:\n{str(e)}")


def create_language_from_template(
    root_path: str, 
    source_code: str, 
    new_code: str, 
    new_name: str, 
    flag_path: str = None,
    styles: Dict[str, Any] = None
) -> bool:
    """
    Create a new language by copying an existing language template.
    
    Args:
        root_path: Root path containing language directories
        source_code: The source language code to copy from (e.g., 'en')
        new_code: The new language code (e.g., 'es')
        new_name: Display name for the new language
        flag_path: Optional path to a flag image file
        styles: Application styles dictionary for notifications
    
    Returns:
        bool: True if successful, False otherwise
    """
    # Validate language codes
    if not new_code or not source_code:
        print("[NewLanguageCreator] Invalid language codes provided")
        return False
    
    # Validate paths
    source_path = os.path.join(root_path, source_code)
    new_path = os.path.join(root_path, new_code)
    
    if not os.path.exists(source_path):
        print(f"[NewLanguageCreator] Source language path does not exist: {source_path}")
        return False
    
    try:
        # Remove destination if it exists (to overwrite)
        if os.path.exists(new_path):
            shutil.rmtree(new_path)
        
        # Copy the entire source directory to the new location
        shutil.copytree(source_path, new_path)
        
        # Update metadata.yaml with new language information
        metadata_path = os.path.join(new_path, "metadata.yaml")
        if os.path.exists(metadata_path):
            import yaml

            # Read the existing metadata
            with open(metadata_path, 'r', encoding='utf-8') as f:
                metadata = yaml.safe_load(f) or {}

            # Update the language metadata
            # NameLanguage should contain the language display name (e.g. "English", "Japanese", etc.)
            metadata['NameLanguage'] = new_name
            # Update Author to current OS username
            import getpass
            current_user = getpass.getuser()
            metadata['Author'] = current_user
            # Version should remain unchanged from the template
            # Remove language_code field as it's not needed in the correct format
            if 'language_code' in metadata:
                del metadata['language_code']
            # Remove name_language field as it's not needed in the correct format
            if 'name_language' in metadata:
                del metadata['name_language']

            # Write back the updated metadata
            with open(metadata_path, 'w', encoding='utf-8') as f:
                yaml.dump(metadata, f, default_flow_style=False, allow_unicode=True, sort_keys=False)
        
        # Copy or replace the flag if provided
        if flag_path and os.path.exists(flag_path):
            destination_flag_path = os.path.join(new_path, "flag.png")
            shutil.copy2(flag_path, destination_flag_path)
        
        print(f"[NewLanguageCreator] Successfully created language '{new_code}' from '{source_code}' at {new_path}")
        return True
        
    except Exception as e:
        print(f"[NewLanguageCreator] Error creating language from template: {e}")
        return False


def create_new_language_dialog(parent_window) -> bool:
    """
    Show dialog to create a new language from an existing template.
    
    Args:
        parent_window: The main window instance with root_localization_path
        
    Returns:
        bool: True if successful, False otherwise
    """
    root_path = getattr(parent_window, 'root_localization_path', None)
    
    if not root_path or not os.path.isdir(root_path):
        color = QColor(parent_window.STYLES['DarkTheme']['NotificationError'])
        parent_window.show_notification(
            "Cannot create new language: No root localization path selected.", 
            color
        )
        return False
    
    # Get available languages by looking at subdirectories
    try:
        available_languages = [
            d for d in os.listdir(root_path) 
            if os.path.isdir(os.path.join(root_path, d)) and d != "language_manifest.json"
        ]
        
        if not available_languages:
            color = QColor(parent_window.STYLES['DarkTheme']['NotificationWarning'])
            parent_window.show_notification(
                "No existing languages found to use as templates.", 
                color
            )
            return False
        
        # Create and show the dialog
        dialog = NewLanguageDialog(parent_window, available_languages, root_path)
        
        def on_language_created(code, path):
            color = QColor(parent_window.STYLES['DarkTheme']['NotificationSuccess'])
            parent_window.show_notification(
                f"New language '{code}' created successfully at: {os.path.basename(path)}", 
                color
            )
            # Refresh the file tree to show the new language
            parent_window.draw_file_tree()
            parent_window.draw_tabs_placeholder()
        
        dialog.language_created.connect(on_language_created)
        dialog.exec_()
        
        return True
        
    except Exception as ex:
        print(f"[NewLanguageCreator] Error showing new language dialog: {ex}")
        color = QColor(parent_window.STYLES['DarkTheme']['NotificationError'])
        parent_window.show_notification(
            f"Failed to show new language dialog: {str(ex)}", 
            color
        )
        return False