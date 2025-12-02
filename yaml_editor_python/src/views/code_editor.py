"""
Custom QPlainTextEdit with line numbers
Based on PyQt5 example for creating a text editor with line numbers
"""
from PyQt5.QtWidgets import QWidget, QPlainTextEdit, QFrame, QLabel, QScrollBar
from PyQt5.QtCore import Qt, QRect
from PyQt5.QtGui import QPainter, QColor, QTextFormat, QFontMetrics


class LineNumberArea(QFrame):
    """Widget to display line numbers"""

    def __init__(self, editor):
        super().__init__(editor)
        self.code_editor = editor
        self.setFrameShape(QFrame.NoFrame)  # Remove any frame
        # Устанавливаем стиль фона из темы
        secondary_bg = editor.styles.get('DarkTheme', {}).get('SecondaryBackground', '#2A2A2A')
        self.setStyleSheet(f"background-color: {secondary_bg};")

    def sizeHint(self):
        return self.code_editor.line_number_area_width(), 0

    def paintEvent(self, event):
        self.code_editor.line_number_paint_event(event)


class CodeEditor(QPlainTextEdit):
    """Custom text editor with line numbers"""

    def __init__(self, styles=None):
        super().__init__()

        self.styles = styles or {}

        # Create line number area
        self.line_numbers = LineNumberArea(self)

        # Initialize line number area width
        self.update_line_number_area_width(0)

        # Set font for line numbers area to match editor
        self.line_numbers.setFont(self.font())

        # Connect signals - correct PyQt5 signals (after initialization)
        self.blockCountChanged.connect(self.update_line_number_area_width)
        self.updateRequest.connect(self.update_line_number_area)
        self.verticalScrollBar().valueChanged.connect(self.update_line_numbers_scroll)

    def line_number_area_width(self):
        """Calculate the width needed for line numbers"""
        digits = len(str(max(1, self.blockCount())))
        # Увеличиваем базовый отступ для большего расстояния между номерами строк и текстом
        space = 25 + self.fontMetrics().width('9') * digits
        return space

    def update_line_number_area_width(self, new_block_count):
        """Update the width of the line number area"""
        self.setViewportMargins(self.line_number_area_width(), 0, 0, 0)

    def update_line_number_area(self, rect, dy):
        """Update the line number area"""
        if dy:
            self.line_numbers.scroll(0, dy)
        else:
            # Update only the changed region
            self.line_numbers.update(0, rect.y(), self.line_numbers.width(), rect.height())

        if rect.contains(self.viewport().rect()):
            self.update_line_number_area_width(0)

    def resizeEvent(self, event):
        """Handle resize events to update line number area"""
        super().resizeEvent(event)

        cr = self.contentsRect()
        self.line_numbers.setGeometry(QRect(cr.left(), cr.top(),
                                          self.line_number_area_width(), cr.height()))

    def line_number_paint_event(self, event):
        """Paint the line numbers"""
        painter = QPainter(self.line_numbers)

        # Fill the background
        background_color = self.styles.get('DarkTheme', {}).get('SecondaryBackground', '#2A2A2A')
        painter.fillRect(event.rect(), QColor(background_color))

        block = self.firstVisibleBlock()
        block_number = block.blockNumber()
        top = self.blockBoundingGeometry(block).translated(self.contentOffset()).top()
        bottom = top + self.blockBoundingRect(block).height()

        # Use foreground color from styles for line numbers
        text_color = self.styles.get('DarkTheme', {}).get('StatusDefault', '#999999')
        painter.setPen(QColor(text_color))  # Color for line numbers

        font = self.font()
        font.setPointSize(font.pointSize() - 1)  # Slightly smaller font for line numbers
        painter.setFont(font)

        while block.isValid() and top <= event.rect().bottom():
            if block.isVisible() and bottom >= event.rect().top():
                number = str(block_number + 1)

                # Calculate alternating background color based on line number
                if (block_number + 1) % 2 == 0:
                    # Even line numbers get a slightly different background
                    alt_bg_color = QColor(self.styles.get('DarkTheme', {}).get('Background', '#1A1A1A'))
                    alt_bg_color.setAlpha(100)  # Make it semi-transparent
                    painter.fillRect(0, int(top), int(self.line_numbers.width()),
                                   int(self.fontMetrics().height()), alt_bg_color)

                # Draw the line number with padding
                painter.drawText(3, int(top), int(self.line_numbers.width()) - 6,
                               int(self.fontMetrics().height()),
                               Qt.AlignRight | Qt.AlignVCenter, number)
            block = block.next()
            top = bottom
            bottom = top + self.blockBoundingRect(block).height()
            block_number += 1

    def update_line_numbers_scroll(self, value):
        """Sync line number area with editor scrolling"""
        self.line_numbers.update()

    def setPlainText(self, text):
        """Override setPlainText to ensure line numbers are updated"""
        super().setPlainText(text)
        self.update_line_number_area_width(0)

    def insertFromMimeData(self, source):
        """Override to make sure line numbers are updated after paste operations"""
        super().insertFromMimeData(source)
        self.update_line_number_area_width(0)

    def setFont(self, font):
        """Override setFont to update line numbers font as well"""
        super().setFont(font)
        self.line_numbers.setFont(font)
        self.update_line_number_area_width(0)

    def update_line_number_styles(self):
        """Update styles for the line number area"""
        secondary_bg = self.styles.get('DarkTheme', {}).get('SecondaryBackground', '#2A2A2A')
        self.line_numbers.setStyleSheet(f"background-color: {secondary_bg};")