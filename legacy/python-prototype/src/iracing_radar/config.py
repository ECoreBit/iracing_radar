from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

Color = tuple[int, int, int, int]


@dataclass(frozen=True)
class WindowConfig:
    width: int = 380
    height: int = 420
    opacity: float = 0.9
    always_on_top: bool = True


@dataclass(frozen=True)
class RadarConfig:
    radar_range_meters: float = 70.0
    proximity_warning_meters: float = 35.0
    full_overlap_meters: float = 5.0
    update_hz: int = 30
    max_cars: int = 10
    window: WindowConfig = WindowConfig()
    colors: dict[str, Color] | None = None


DEFAULT_COLORS: dict[str, Color] = {
    "background": (8, 12, 18, 160),
    "grid": (255, 255, 255, 42),
    "text": (238, 244, 255, 235),
    "own_car": (50, 170, 255, 255),
    "car_far": (255, 180, 65, 220),
    "car_near": (255, 75, 75, 245),
    "side_confirmed": (255, 45, 45, 230),
    "side_unknown": (255, 145, 30, 220),
}


def _color(value: object, fallback: Color) -> Color:
    if not isinstance(value, list) or len(value) != 4:
        return fallback
    try:
        return tuple(max(0, min(255, int(v))) for v in value)  # type: ignore[return-value]
    except (TypeError, ValueError):
        return fallback


def load_config(path: Path) -> RadarConfig:
    if not path.exists():
        return RadarConfig(colors=DEFAULT_COLORS)

    data = json.loads(path.read_text(encoding="utf-8-sig"))
    window_data = data.get("window", {})
    color_data = data.get("colors", {})

    return RadarConfig(
        radar_range_meters=float(data.get("radar_range_meters", 70)),
        proximity_warning_meters=float(data.get("proximity_warning_meters", 35)),
        full_overlap_meters=float(data.get("full_overlap_meters", 5)),
        update_hz=int(data.get("update_hz", 30)),
        max_cars=int(data.get("max_cars", 10)),
        window=WindowConfig(
            width=int(window_data.get("width", 380)),
            height=int(window_data.get("height", 420)),
            opacity=float(window_data.get("opacity", 0.9)),
            always_on_top=bool(window_data.get("always_on_top", True)),
        ),
        colors={name: _color(color_data.get(name), fallback) for name, fallback in DEFAULT_COLORS.items()},
    )
