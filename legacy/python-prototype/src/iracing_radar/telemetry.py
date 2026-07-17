from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Any

import irsdk


@dataclass(frozen=True)
class NearbyCar:
    car_idx: int
    car_number: str
    relative_meters: float
    abs_meters: float


@dataclass(frozen=True)
class RadarSnapshot:
    connected: bool
    on_track: bool
    track_length_meters: float
    left_right_state: int
    nearby_cars: list[NearbyCar]
    status: str


class IRacingTelemetry:
    def __init__(self) -> None:
        self._ir = irsdk.IRSDK()
        self._started = False

    def snapshot(self, radar_range_meters: float, max_cars: int) -> RadarSnapshot:
        if not self._ensure_started():
            return RadarSnapshot(False, False, 0.0, 0, [], "waiting for iRacing")

        if not self._is_connected():
            self.shutdown()
            return RadarSnapshot(False, False, 0.0, 0, [], "not connected")

        try:
            self._ir.freeze_var_buffer_latest()
        except Exception:
            self.shutdown()
            return RadarSnapshot(False, False, 0.0, 0, [], "telemetry buffer unavailable")

        player_idx = self._read_int("PlayerCarIdx", -1)
        track_length = self._track_length_meters()
        player_dist_pct = self._read_float_at("CarIdxLapDistPct", player_idx)
        left_right_state = self._read_int("CarLeftRight", 0)

        if player_idx < 0 or player_dist_pct is None or track_length <= 0:
            return RadarSnapshot(True, False, track_length, left_right_state, [], "waiting for track data")

        car_dist_pct = self._read_array("CarIdxLapDistPct") or []
        car_numbers = self._car_numbers()
        nearby: list[NearbyCar] = []

        for car_idx, dist_pct in enumerate(car_dist_pct):
            if car_idx == player_idx:
                continue
            if dist_pct is None:
                continue
            try:
                dist_pct_float = float(dist_pct)
            except (TypeError, ValueError):
                continue
            if dist_pct_float < 0:
                continue

            relative = self._relative_meters(float(player_dist_pct), dist_pct_float, track_length)
            if abs(relative) > radar_range_meters:
                continue

            nearby.append(
                NearbyCar(
                    car_idx=car_idx,
                    car_number=car_numbers.get(car_idx, str(car_idx)),
                    relative_meters=relative,
                    abs_meters=abs(relative),
                )
            )

        nearby.sort(key=lambda car: car.abs_meters)
        return RadarSnapshot(True, True, track_length, left_right_state, nearby[:max_cars], "ok")

    def shutdown(self) -> None:
        if self._started:
            try:
                self._ir.shutdown()
            finally:
                self._started = False

    def _ensure_started(self) -> bool:
        if self._started and getattr(self._ir, "is_initialized", False):
            return True
        try:
            self._started = bool(self._ir.startup())
        except Exception:
            self._started = False
        if not self._started:
            self.shutdown()
        return self._started

    def _is_connected(self) -> bool:
        try:
            return bool(self._ir.is_connected)
        except Exception:
            return False

    def _read_array(self, key: str) -> list[Any] | None:
        try:
            value = self._ir[key]
        except Exception:
            return None
        return value if isinstance(value, list) else None

    def _read_float_at(self, key: str, index: int) -> float | None:
        values = self._read_array(key)
        if values is None or index < 0 or index >= len(values):
            return None
        try:
            return float(values[index])
        except (TypeError, ValueError):
            return None

    def _read_int(self, key: str, fallback: int = 0) -> int:
        try:
            return int(self._ir[key])
        except Exception:
            return fallback

    def _read_session_dict(self, key: str) -> dict[str, Any]:
        try:
            value = self._ir[key]
        except Exception:
            return {}
        return value if isinstance(value, dict) else {}

    def _track_length_meters(self) -> float:
        weekend_info = self._read_session_dict("WeekendInfo")
        raw = str(weekend_info.get("TrackLength", ""))
        match = re.search(r"([0-9.]+)\s*(km|mi)", raw, flags=re.IGNORECASE)
        if not match:
            return 0.0
        value = float(match.group(1))
        unit = match.group(2).lower()
        return value * 1609.344 if unit == "mi" else value * 1000.0

    def _car_numbers(self) -> dict[int, str]:
        driver_info = self._read_session_dict("DriverInfo")
        drivers = driver_info.get("Drivers") or []
        result: dict[int, str] = {}
        for driver in drivers:
            try:
                car_idx = int(driver.get("CarIdx"))
            except (TypeError, ValueError):
                continue
            number = driver.get("CarNumberRaw") or driver.get("CarNumber") or car_idx
            result[car_idx] = str(number)
        return result

    @staticmethod
    def _relative_meters(player_pct: float, target_pct: float, track_length_meters: float) -> float:
        delta = target_pct - player_pct
        if delta > 0.5:
            delta -= 1.0
        elif delta < -0.5:
            delta += 1.0
        return delta * track_length_meters
