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
    last = None
    print("Watching CarLeftRight. Press Ctrl+C to stop.")
    try:
        while True:
            snapshot = telemetry.snapshot(80, 16)
            state = snapshot.left_right_state
            if state != last:
                label = LABELS.get(state, f"UNKNOWN {state}")
                print(f"{time.strftime('%H:%M:%S')} connected={snapshot.connected} on_track={snapshot.on_track} CarLeftRight={state} {label}")
                last = state
            time.sleep(0.05)
    except KeyboardInterrupt:
        print("Stopped.")
    finally:
        telemetry.shutdown()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
