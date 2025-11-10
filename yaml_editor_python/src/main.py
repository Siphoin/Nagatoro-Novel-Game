# main.py
import sys
from PyQt5.QtWidgets import QApplication

# После создания __init__.py в папке src, этот импорт работает надежно!
from view import YAMLEditorWindow

def main():
    # 1. Инициализация QApplication
    app = QApplication(sys.argv)
    
    # 2. Создание и отображение главного окна
    editor_window = YAMLEditorWindow()
    editor_window.show()
    
    # 3. Запуск основного цикла событий
    sys.exit(app.exec_())

if __name__ == '__main__':
    main()