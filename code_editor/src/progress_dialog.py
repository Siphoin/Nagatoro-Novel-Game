"""
Abstract progress/status module for displaying operation progress
"""

from PyQt5.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QLabel, QProgressBar, QPushButton, QFrame
from PyQt5.QtCore import QObject, pyqtSignal, QThread
from PyQt5.QtGui import QFont, QPainter, QColor, QLinearGradient
from PyQt5.QtCore import Qt


class StyledProgressBar(QFrame):
    """
    Custom styled progress bar that integrates with the application's style system
    """
    def __init__(self, parent=None, styles=None):
        super().__init__(parent)
        self._value = 0
        self._maximum = 100
        self._styles = styles or {}

        # Set minimum size and fixed height
        self.setMinimumHeight(20)
        self.setFixedHeight(20)

        # Set object name for CSS styling
        self.setObjectName("StyledProgressBar")

    def set_value(self, value):
        """Set the progress value"""
        self._value = max(0, min(value, self._maximum))
        self.update()

    def set_maximum(self, maximum):
        """Set the maximum value"""
        self._maximum = maximum
        # Adjust value if needed
        if self._value > maximum:
            self._value = maximum
        self.update()

    def set_styles(self, styles):
        """Update the styles for this progress bar"""
        self._styles = styles
        self.update()

    def paintEvent(self, event):
        """Custom paint event to draw styled progress bar"""
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing)

        # Calculate progress percentage
        if self._maximum > 0:
            progress_percent = self._value / self._maximum
        else:
            progress_percent = 0

        # Get colors from styles
        theme = self._styles.get('DarkTheme', {})
        bg_color = QColor(theme.get('ProgressBarBackground', '#1F1F1F'))
        progress_color = QColor(theme.get('ProgressBarColor', '#C84B31'))
        border_color = QColor(theme.get('ProgressBarBorderColor', '#3A3A3A'))

        # Draw background
        painter.fillRect(self.rect(), bg_color)

        # Draw border
        painter.setPen(border_color)
        painter.drawRect(self.rect().adjusted(0, 0, -1, -1))

        # Draw progress fill
        if progress_percent > 0:
            progress_width = int(self.width() * progress_percent)
            progress_rect = self.rect().adjusted(1, 1, -(self.width() - progress_width) - 1, -1)

            # Create gradient for the progress fill
            gradient = QLinearGradient(0, 0, progress_width, 0)
            gradient.setColorAt(0, progress_color)
            gradient.setColorAt(1, QColor(progress_color).darker(120))  # Slightly darker

            painter.fillRect(progress_rect, gradient)


class ProgressDialog(QDialog):
    """
    A dialog that displays operation progress and status
    """
    def __init__(self, parent=None, title="Operation in Progress", styles=None):
        super().__init__(parent)
        self.setWindowTitle(title)
        self.setModal(True)
        self.resize(400, 120)

        layout = QVBoxLayout()

        # Status label
        self.status_label = QLabel("Initializing...")
        font = QFont()
        font.setPointSize(10)
        self.status_label.setFont(font)
        layout.addWidget(self.status_label)

        # Custom styled progress bar
        self.progress_bar = StyledProgressBar(parent=self, styles=styles or {})
        layout.addWidget(self.progress_bar)

        # Cancel button
        button_layout = QHBoxLayout()
        button_layout.addStretch()
        self.cancel_button = QPushButton("Cancel")
        self.cancel_button.clicked.connect(self.cancel_operation)
        button_layout.addWidget(self.cancel_button)

        layout.addLayout(button_layout)
        self.setLayout(layout)

        # Operation control flags
        self._cancelled = False

    def set_styles(self, styles):
        """Update the styles for the progress bar"""
        self.progress_bar.set_styles(styles)

    def update_status(self, message):
        """Update the status message"""
        self.status_label.setText(message)
        self.status_label.update()

    def update_progress(self, value, max_value=100):
        """Update the progress bar"""
        self.progress_bar.set_maximum(max_value)
        self.progress_bar.set_value(value)

        self.progress_bar.update()

    def cancel_operation(self):
        """Handle cancellation request"""
        self._cancelled = True
        self.cancel_button.setText("Cancelling...")
        self.cancel_button.setEnabled(False)

    def is_cancelled(self):
        """Check if operation was cancelled"""
        return self._cancelled


class ProgressWorker(QObject):
    """
    Abstract worker class for background operations with progress reporting
    """
    # Signals for communication with main thread
    progress_updated = pyqtSignal(int, int, str)  # current, total, status
    operation_started = pyqtSignal()
    operation_finished = pyqtSignal(bool, str)  # success, message
    status_updated = pyqtSignal(str)

    def __init__(self):
        super().__init__()
        self._cancelled = False

    def cancel(self):
        """Request cancellation of the operation"""
        self._cancelled = True

    def is_cancelled(self):
        """Check if operation should be cancelled"""
        return self._cancelled

    def run_operation(self):
        """
        Override this method to implement the actual operation.
        Should periodically check self.is_cancelled() and emit progress updates.
        """
        raise NotImplementedError("Subclasses must implement run_operation method")


class OperationRunner:
    """
    Helper class to run operations with progress dialog
    """
    def __init__(self, parent_window, operation_name="Operation"):
        self.parent_window = parent_window
        self.operation_name = operation_name
        self.thread = None
        self.worker = None
        self.dialog = None

    def run_with_progress(self, worker_class, *args, **kwargs):
        """
        Run an operation with progress dialog
        :param worker_class: Worker class that inherits from ProgressWorker
        :param args: Arguments to pass to the worker constructor
        :param kwargs: Keyword arguments to pass to the worker constructor
        """
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
        self.worker.operation_finished.connect(self._on_operation_finished)
        self.dialog.cancel_button.clicked.connect(self.worker.cancel)

        # Start thread and operation
        self.thread.started.connect(self.worker.run_operation)
        self.dialog.show()
        self.thread.start()

    def _on_operation_finished(self, success, message):
        """Handle operation completion"""
        if self.thread:
            self.thread.quit()
            self.thread.wait()

        if self.dialog:
            self.dialog.close()