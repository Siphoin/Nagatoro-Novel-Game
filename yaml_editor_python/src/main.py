# main.py
import sys
import os
import traceback

# Import PyQt5 first
from PyQt5.QtWidgets import QApplication

# Add the src directory to the path so imports work correctly
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from view import YAMLEditorWindow

def main():
    # 1. QApplication initialization
    app = QApplication(sys.argv)

    # 2. Create and display the main window
    try:
        editor_window = YAMLEditorWindow()
        editor_window.show()
        print("Window created successfully. Starting event loop...")
    except Exception as e:
        print("Error creating window:")
        traceback.print_exc()
        return

    # 3. Start the main event loop
    sys.exit(app.exec_())

if __name__ == '__main__':
    main()