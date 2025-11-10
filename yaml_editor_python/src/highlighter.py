# src/highlighter.py
from PyQt5.QtGui import QSyntaxHighlighter, QTextCharFormat, QColor, QFont
from PyQt5.QtCore import Qt, QRegularExpression
import re
from typing import Dict, Any

# --- Константы стилей (на основе скриншота и темной темы) ---
# В реальном проекте это должно загружаться из YamlSyntaxStyle
# Используем темные цвета, близкие к Unity Editor
class YamlSyntaxStyle:
    def __init__(self):
        # Цвета в формате HEX, используемые в Rich Text и адаптированные для Qt
        self.comment_color = "#608B4E"  # Ярко-зеленый
        self.key_color = "#C8A33E"      # Желто-коричневый
        self.keyword_color = "#AF55C4"  # Фиолетовый (для булевых/null)
        self.string_color = "#32B7FF"   # Ярко-голубой (для строк и тире в списках)
        self.default_color = "#CCCCCC"  # Светло-серый (для обычного текста)


class YamlHighlighter(QSyntaxHighlighter):
    def __init__(self, document):
        super().__init__(document)
        self.styles = YamlSyntaxStyle()
        self.highlighting_rules = []
        self.initialize_formats()

    def initialize_formats(self):
        # 1. Формат для комментариев (CommentColor)
        comment_format = QTextCharFormat()
        comment_format.setForeground(QColor(self.styles.comment_color))
        # Регулярное выражение для комментария: начинается с # и идет до конца строки
        self.highlighting_rules.append((QRegularExpression("#.*"), comment_format))

        # 2. Формат для ключей (KeyColor) - Используем его в методе highlightBlock

        # 3. Формат для ключевых слов (KeywordColor) - true, false, null, числа
        keyword_format = QTextCharFormat()
        keyword_format.setForeground(QColor(self.styles.keyword_color))
        keywords = ["true", "false", "null", "True", "False", "NULL"]
        for word in keywords:
            # Границы слова, чтобы не подсвечивать 'true' внутри 'something-true'
            pattern = QRegularExpression(r'\b' + re.escape(word) + r'\b')
            self.highlighting_rules.append((pattern, keyword_format))

        # 4. Формат для строк и тире списка (StringColor)
        string_format = QTextCharFormat()
        string_format.setForeground(QColor(self.styles.string_color))
        
        # Строки в кавычках (одиночные или двойные)
        # Это сложно, лучше обрабатывать в highlightBlock
        
        # Тире списка в начале строки
        list_dash_format = QTextCharFormat()
        list_dash_format.setForeground(QColor(self.styles.string_color))
        # Паттерн для тире списка: ^(\s*)-(\s*)\S
        self.highlighting_rules.append((QRegularExpression(r"^\s*-\s*"), list_dash_format))


    def highlightBlock(self, text: str):
        """
        Основной метод, переопределяющий подсветку строки.
        Здесь реализуется логика ApplyYamlHighlighting, но через QSyntaxHighlighter.
        """
        # Применяем правила на основе QRegularExpression
        for pattern, format in self.highlighting_rules:
            it = pattern.globalMatch(text)
            while it.hasNext():
                match = it.next()
                # Применяем формат ко всей найденной части
                self.setFormat(match.capturedStart(), match.capturedLength(), format)


        # --- Сложная логика: Ключи и Значения (Аналог ApplyYamlHighlighting) ---

        default_format = QTextCharFormat()
        default_format.setForeground(QColor(self.styles.default_color))
        key_format = QTextCharFormat()
        key_format.setForeground(QColor(self.styles.key_color))
        string_format = QTextCharFormat()
        string_format.setForeground(QColor(self.styles.string_color))

        
        # 1. Поиск комментария, чтобы не анализировать его
        # Используем регэксп, имитирующий IndexOfHashOutsideQuotes
        comment_match = re.search(r'#.*', text)
        content = text
        comment_start = -1
        if comment_match:
            # Находим индекс решетки, убеждаемся, что она вне кавычек
            try:
                comment_index = self.index_of_hash_outside_quotes(text)
                if comment_index >= 0:
                    comment_start = comment_index
                    content = text[:comment_index]
            except:
                # В случае ошибки оставляем всю строку, чтобы не сломать приложение
                pass


        # 2. Поиск разделителя (:)
        colon_index = self.index_of_char_outside_quotes(content, ':')

        if colon_index >= 0:
            # Мы нашли пару "ключ: значение"
            key_text = content[:colon_index].strip()
            value_text = content[colon_index + 1:].lstrip()

            # --- Подсветка Ключа (KeyColor) ---
            # Находим фактическое начало ключа, пропуская отступы
            key_start_in_line = text.find(key_text.lstrip()) 
            
            if key_start_in_line != -1 and key_text.strip():
                # Длина ключа
                key_end_in_line = key_start_in_line + len(key_text.strip())
                
                # Применяем формат ключа
                self.setFormat(key_start_in_line, len(key_text.strip()), key_format)

            # --- Подсветка Значения (StringColor/KeywordColor) ---
            value_start = text.find(value_text, colon_index + 1)
            
            if value_start != -1 and value_text:
                # Если значение в кавычках (строка)
                if value_text.startswith('"') and value_text.endswith('"') or \
                   value_text.startswith("'") and value_text.endswith("'"):
                    self.setFormat(value_start, len(value_text), string_format)
                
                # Если это числа/булевы (нужно использовать QRegularExpression для точности)
                elif self.is_yaml_bool_or_null(value_text) or self.is_yaml_number(value_text):
                    # Применяем ранее определенный KeywordFormat
                    keyword_format = self.highlighting_rules[2][1] # Получаем формат из rules
                    self.setFormat(value_start, len(value_text), keyword_format)
                
                # Остальное - не в кавычках и не ключевые слова/числа (StringColor)
                else:
                    self.setFormat(value_start, len(value_text), string_format)

        
        # --- Подсветка Тире Списка (Dash - уже сделана в initialize_formats) ---
        # Правило `list_dash_format` уже покрывает тире в начале строки.


    # --- Портированные вспомогательные методы из C# ---

    def index_of_hash_outside_quotes(self, line: str) -> int:
        """Поиск # вне кавычек (IndexOfHashOutsideQuotes)."""
        in_single = False
        in_double = False
        for i, c in enumerate(line):
            if c == '\'' and not in_double:
                in_single = not in_single
            elif c == '"' and not in_single:
                in_double = not in_double
            elif c == '#' and not in_single and not in_double:
                return i
        return -1

    def index_of_char_outside_quotes(self, line: str, target: str) -> int:
        """Поиск символа вне кавычек (IndexOfCharOutsideQuotes)."""
        in_single = False
        in_double = False
        for i, c in enumerate(line):
            if c == '\'' and not in_double:
                in_single = not in_single
            elif c == '"' and not in_single:
                in_double = not in_double
            elif c == target and not in_single and not in_double:
                return i
        return -1
    
    def is_yaml_number(self, s: str) -> bool:
        """Проверка на число (IsYamlNumber)."""
        return re.match(r"^[+-]?\d+(\.\d+)?$", s.strip()) is not None

    def is_yaml_bool_or_null(self, s: str) -> bool:
        """Проверка на булево или null (IsYamlBoolOrNull)."""
        token = s.strip()
        return token in ["true", "false", "null", "True", "False", "NULL"]