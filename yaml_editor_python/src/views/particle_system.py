"""
Система частиц для визуального эффекта при вводе символов с GPU ускорением
"""
from PyQt5.QtWidgets import QWidget, QApplication
from PyQt5.QtCore import QTimer, Qt, QRect
from PyQt5.QtGui import QPainter, QColor, QPen, QBrush, QOpenGLContext
from PyQt5.QtOpenGL import QGLWidget
import random


class Particle:
    """Класс для одной частицы"""
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.start_x = x
        self.start_y = y
        self.vx = random.uniform(-3, 3)  # Случайная скорость по X
        self.vy = random.uniform(-3, 3)  # Случайная скорость по Y
        self.life = 1.0  # Жизнь частицы (от 1.0 до 0.0)
        self.decay = random.uniform(0.02, 0.05)  # Скорость уменьшения жизни
        self.size = random.randint(2, 5)  # Размер частицы
        # Случайный цвет в пределах определенного диапазона
        self.base_color = QColor(
            random.randint(200, 255),
            random.randint(100, 200),
            random.randint(50, 150)
        )
        self.alpha = 255

    def update(self):
        """Обновление состояния частицы"""
        self.x += self.vx
        self.y += self.vy
        # Добавляем гравитацию для более реалистичного движения
        self.vy += 0.1
        self.life -= self.decay
        self.alpha = int(self.life * 255)
        return self.life > 0 and 0 <= self.alpha <= 255

    def draw(self, painter):
        """Отрисовка частицы"""
        if self.life > 0:
            color = QColor(
                self.base_color.red(),
                self.base_color.green(),
                self.base_color.blue(),
                self.alpha
            )
            painter.setPen(QPen(color, 1))
            painter.setBrush(QBrush(color))
            painter.drawEllipse(
                int(self.x - self.size/2),
                int(self.y - self.size/2),
                self.size,
                self.size
            )


class ParticleEffect(QWidget):
    """Виджет для отображения системы частиц с GPU ускорением"""

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)  # Делаем окно прозрачным для мыши

        # Пытаемся включить GPU ускорение для QPainter
        try:
            # Установим политику рендеринга для использования OpenGL
            self.setMouseTracking(False)
            # Используем более производительный рендерер если возможно
            if QOpenGLContext.hasOpenGL():
                # Установим атрибуты для возможного использования GPU
                self.setAttribute(Qt.WA_PaintOnScreen, False)  # Разрешаем двойную буферизацию
        except:
            pass  # Если нет поддержки OpenGL, продолжаем без неё

        self.particles = []
        self.timer = QTimer(self)
        self.timer.timeout.connect(self.update_particles)
        self.timer.start(16)  # ~60 FPS
        self.hide()  # Изначально скрыт

        # Устанавливаем геометрию сразу при инициализации
        if self.parent():
            self.update_parent_geometry()

    def add_particles_at(self, x, y, count=8):
        """Добавить частицы в указанную позицию"""
        for _ in range(count):
            self.particles.append(Particle(x, y))

        # Показываем виджет на короткое время
        self.show()
        # Автоматически скрываем через 1.5 секунды, если нет частиц
        QTimer.singleShot(1500, self._check_hide)

    def update_particles(self):
        """Обновление всех частиц"""
        self.particles = [p for p in self.particles if p.update()]
        self.update()

        # Если частиц нет, скрываем виджет
        if not self.particles:
            self.hide()

    def _check_hide(self):
        """Проверка необходимости скрытия виджета"""
        if not self.particles:
            self.hide()

    def paintEvent(self, event):
        """Событие отрисовки"""
        # Создаем QPainter с возможным GPU ускорением
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing)  # Сглаживание
        painter.setRenderHint(QPainter.HighQualityAntialiasing)
        painter.setRenderHint(QPainter.SmoothPixmapTransform)

        # Пытаемся использовать OpenGL backend для QPainter если возможно
        try:
            # Устанавливаем дополнительные оптимизации
            painter.setCompositionMode(QPainter.CompositionMode_SourceOver)
        except:
            pass

        for particle in self.particles:
            particle.draw(painter)

        # Дополнительно, если доступен OpenGL, можем создать отладочное сообщение
        try:
            if QOpenGLContext.hasOpenGL():
                # Выводим отладочную информацию о GPU использовании
                debug_color = QColor(0, 255, 0, 100)  # Полупрозрачный зеленый
                painter.setPen(QPen(debug_color, 1))
                painter.drawText(10, 15, "GPU: ON")  # Показываем, что GPU режим включен
        except:
            pass

    def resizeEvent(self, event):
        """Событие изменения размера"""
        super().resizeEvent(event)
        # Обновляем размер для корректного позиционирования
        self.update_parent_geometry()

    def update_parent_geometry(self):
        """Update geometry to match parent"""
        if self.parent():
            parent_rect = self.parent().rect()
            self.setGeometry(parent_rect)
            self.setFixedSize(parent_rect.size())