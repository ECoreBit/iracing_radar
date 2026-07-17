from __future__ import annotations

import sys
import time

sys.path.insert(0, "src/iracing_radar")
from telemetry import IRacingTelemetry  # noqa: E402

LABELS = {
    0: "OFF",
    1: "CLEAR",
    2: "CAR LEFT",
    3: "CAR RIGHT",
    4: "LEFT + RIGHT",
    5: "2 CARS LEFT",
    6: "2 CARS RIGHT",
}


def main() -> int:
    telemetry = IRacingTelemetry()
    print("Watching nearest cars + CarLeftRight. Press Ctrl+C to stop.")
    try:
        while True:
            snapshot = telemetry.snapshot(80, 16)
            label = LABELS.get(snapshot.left_right_state, f"UNKNOWN {snapshot.left_right_state}")
            cars = ", ".join(
                f"#{car.car_number}:{car.relative_meters:+.1f}m"
                for car in snapshot.nearby_cars[:6]
            ) or "no cars within 80m"
            print(
                f"{time.strftime('%H:%M:%S')} "
                f"connected={snapshot.connected} on_track={snapshot.on_track} "
                f"LR={snapshot.left_right_state} {label} | {cars}"
            )
            time.sleep(1.0)
    except KeyboardInterrupt:
        print("Stopped.")
    finally:
        telemetry.shutdown()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
