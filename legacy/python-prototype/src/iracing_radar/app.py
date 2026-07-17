from __future__ import annotations

import sys
import time
from pathlib import Path

from PySide6.QtCore import QPointF, QRectF, Qt, QTimer
from PySide6.QtGui import QColor, QFont, QPainter, QPen
from PySide6.QtWidgets import QApplication, QWidget

from config import RadarConfig, load_config
from telemetry import IRacingTelemetry, RadarSnapshot

ROOT = Path(__file__).resolve().parents[2]

LR_LABELS = {
    0: "OFF",
    1: "CLEAR",
    2: "CAR LEFT",
    3: "CAR RIGHT",
    4: "LEFT + RIGHT",
    5: "2 LEFT",
    6: "2 RIGHT",
}
LEFT_STATES = {2, 4, 5}
RIGHT_STATES = {3, 4, 6}
WARNING_STATES = LEFT_STATES | RIGHT_STATES


def qcolor(config: RadarConfig, name: str) -> QColor:
    r, g, b, a = (config.colors or {})[name]
    return QColor(r, g, b, a)


class RadarOverlay(QWidget):
    def __init__(self, config: RadarConfig) -> None:
        super().__init__()
        self.config = config
        self.telemetry = IRacingTelemetry()
        self.snapshot = RadarSnapshot(False, False, 0.0, 0, [], "starting")
        self._held_lr_state = 0
        self._held_lr_until = 0.0
        self._held_unknown_until = 0.0

        flags = Qt.WindowType.FramelessWindowHint | Qt.WindowType.Tool
        if config.window.always_on_top:
            flags |= Qt.WindowType.WindowStaysOnTopHint
        self.setWindowFlags(flags)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self.setAttribute(Qt.WidgetAttribute.WA_ShowWithoutActivating, True)
        self.setWindowOpacity(config.window.opacity)
        self.resize(config.window.width, config.window.height)

        self.timer = QTimer(self)
        self.timer.timeout.connect(self.refresh)
        self.timer.start(max(10, int(1000 / max(1, config.update_hz))))

    def refresh(self) -> None:
        self.snapshot = self.telemetry.snapshot(self.config.radar_range_meters, self.config.max_cars)
        self._update_holds()
        self.update()

    def _update_holds(self) -> None:
        now = time.monotonic()
        if self.snapshot.left_right_state in WARNING_STATES:
            self._held_lr_state = self.snapshot.left_right_state
            self._held_lr_until = now + 0.85
        elif now >= self._held_lr_until:
            self._held_lr_state = self.snapshot.left_right_state

        if self._has_proximity_risk() and self.snapshot.left_right_state not in WARNING_STATES:
            self._held_unknown_until = now + 0.85

    def closeEvent(self, event) -> None:  # noqa: ANN001
        self.telemetry.shutdown()
        super().closeEvent(event)

    def paintEvent(self, event) -> None:  # noqa: ANN001
        del event
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)

        outer = self.rect().adjusted(8, 8, -8, -8)
        painter.setBrush(qcolor(self.config, "background"))
        painter.setPen(Qt.PenStyle.NoPen)
        painter.drawRoundedRect(outer, 24, 24)

        center = QPointF(outer.center().x(), outer.center().y() - 8)
        radius = min(outer.width(), outer.height() - 52) / 2

        self._draw_header(painter, outer)
        self._draw_grid(painter, center, radius)
        self._draw_side_alerts(painter, center, radius)
        self._draw_own_car(painter, center)

        if self.snapshot.connected and self.snapshot.on_track:
            self._draw_cars(painter, center, radius)
        else:
            self._draw_message(painter, self.snapshot.status.upper())

        self._draw_footer(painter, outer)

    def _draw_header(self, painter: QPainter, outer: QRectF) -> None:
        state = self._effective_lr_state()
        label = LR_LABELS.get(self.snapshot.left_right_state, f"LR {self.snapshot.left_right_state}")
        effective = LR_LABELS.get(state, f"LR {state}")
        if state != self.snapshot.left_right_state:
            label = f"{label} / HOLD {effective}"
        if self._unknown_side_active() and state not in WARNING_STATES:
            label = f"{label} / SIDE UNKNOWN"

        painter.setPen(qcolor(self.config, "text"))
        painter.setFont(QFont("Segoe UI", 10, QFont.Weight.Bold))
        painter.drawText(outer.adjusted(10, 7, -10, -outer.height() + 32), Qt.AlignmentFlag.AlignCenter, label)

    def _draw_grid(self, painter: QPainter, center: QPointF, radius: float) -> None:
        painter.setPen(QPen(qcolor(self.config, "grid"), 1))
        for scale in (0.33, 0.66, 1.0):
            painter.drawEllipse(center, radius * scale, radius * scale)
        painter.drawLine(QPointF(center.x(), center.y() - radius), QPointF(center.x(), center.y() + radius))

        painter.setPen(qcolor(self.config, "text"))
        painter.setFont(QFont("Segoe UI", 8, QFont.Weight.Bold))
        painter.drawText(QRectF(center.x() - 35, center.y() - radius + 17, 70, 16), Qt.AlignmentFlag.AlignCenter, "FRONT")
        painter.drawText(QRectF(center.x() - 35, center.y() + radius - 31, 70, 16), Qt.AlignmentFlag.AlignCenter, "REAR")

    def _draw_own_car(self, painter: QPainter, center: QPointF) -> None:
        painter.setBrush(qcolor(self.config, "own_car"))
        painter.setPen(Qt.PenStyle.NoPen)
        painter.drawRoundedRect(QRectF(center.x() - 15, center.y() - 25, 30, 50), 7, 7)

    def _draw_cars(self, painter: QPainter, center: QPointF, radius: float) -> None:
        for car in self.snapshot.nearby_cars:
            y = center.y() - max(-1.0, min(1.0, car.relative_meters / self.config.radar_range_meters)) * radius * 0.83
            near = car.abs_meters <= self.config.proximity_warning_meters
            painter.setBrush(qcolor(self.config, "car_near" if near else "car_far"))
            painter.setPen(Qt.PenStyle.NoPen)
            painter.drawRoundedRect(QRectF(center.x() - 13, y - 20, 26, 40), 6, 6)

            painter.setPen(qcolor(self.config, "text"))
            painter.setFont(QFont("Segoe UI", 8, QFont.Weight.Bold))
            painter.drawText(QRectF(center.x() - 58, y - 35, 116, 16), Qt.AlignmentFlag.AlignCenter, f"#{car.car_number} {car.relative_meters:+.0f}m")

    def _draw_side_alerts(self, painter: QPainter, center: QPointF, radius: float) -> None:
        strength = self._proximity_strength()
        height = 88 + 124 * max(strength, 0.22)
        left = QRectF(center.x() - radius - 8, center.y() - height / 2, 62, height)
        right = QRectF(center.x() + radius - 54, center.y() - height / 2, 62, height)

        state = self._effective_lr_state()
        if state in WARNING_STATES:
            painter.setBrush(qcolor(self.config, "side_confirmed"))
            painter.setPen(Qt.PenStyle.NoPen)
            if state in LEFT_STATES:
                self._draw_side_box(painter, left, "L", 26)
            if state in RIGHT_STATES:
                self._draw_side_box(painter, right, "R", 26)
            return

        if self._unknown_side_active():
            painter.setBrush(qcolor(self.config, "side_unknown"))
            painter.setPen(Qt.PenStyle.NoPen)
            self._draw_side_box(painter, left, "L?", 22)
            self._draw_side_box(painter, right, "R?", 22)

    def _draw_side_box(self, painter: QPainter, rect: QRectF, text: str, size: int) -> None:
        painter.drawRoundedRect(rect, 16, 16)
        painter.setPen(QColor(255, 255, 255, 250))
        painter.setFont(QFont("Segoe UI", size, QFont.Weight.Black))
        painter.drawText(rect, Qt.AlignmentFlag.AlignCenter, text)

    def _draw_message(self, painter: QPainter, message: str) -> None:
        painter.setPen(qcolor(self.config, "text"))
        painter.setFont(QFont("Segoe UI", 13, QFont.Weight.Bold))
        painter.drawText(self.rect(), Qt.AlignmentFlag.AlignCenter, message)

    def _draw_footer(self, painter: QPainter, outer: QRectF) -> None:
        cars = " | ".join(f"#{c.car_number} {c.relative_meters:+.1f}m" for c in self.snapshot.nearby_cars[:4])
        if not cars:
            cars = "no cars in range"
        label = LR_LABELS.get(self.snapshot.left_right_state, "UNKNOWN")

        panel = QRectF(outer.left() + 8, outer.bottom() - 58, outer.width() - 16, 48)
        painter.setBrush(QColor(0, 0, 0, 135))
        painter.setPen(Qt.PenStyle.NoPen)
        painter.drawRoundedRect(panel, 10, 10)
        painter.setPen(qcolor(self.config, "text"))
        painter.setFont(QFont("Consolas", 9, QFont.Weight.Bold))
        painter.drawText(panel.adjusted(10, 5, -10, -25), Qt.AlignmentFlag.AlignLeft, f"RAW LR={self.snapshot.left_right_state} {label}")
        painter.drawText(panel.adjusted(10, 25, -10, -5), Qt.AlignmentFlag.AlignLeft, cars)

    def _effective_lr_state(self) -> int:
        if time.monotonic() < self._held_lr_until:
            return self._held_lr_state
        return self.snapshot.left_right_state

    def _unknown_side_active(self) -> bool:
        return time.monotonic() < self._held_unknown_until

    def _has_proximity_risk(self) -> bool:
        return any(car.abs_meters <= self.config.proximity_warning_meters for car in self.snapshot.nearby_cars)

    def _proximity_strength(self) -> float:
        if not self.snapshot.nearby_cars:
            return 0.0
        closest = min(car.abs_meters for car in self.snapshot.nearby_cars)
        if closest <= self.config.full_overlap_meters:
            return 1.0
        if closest >= self.config.proximity_warning_meters:
            return 0.0
        span = self.config.proximity_warning_meters - self.config.full_overlap_meters
        return 1.0 - ((closest - self.config.full_overlap_meters) / span)


def main() -> int:
    app = QApplication(sys.argv)
    overlay = RadarOverlay(load_config(ROOT / "config.json"))
    overlay.show()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
