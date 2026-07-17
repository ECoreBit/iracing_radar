from __future__ import annotations

from pathlib import Path
from urllib import error, request

import irsdk

SIM_STATUS_URL = "http://127.0.0.1:32034/get_sim_status?object=simStatus"


def check_app_ini() -> None:
    app_ini = Path.home() / "Documents" / "iRacing" / "app.ini"
    print(f"app.ini: {app_ini}")
    if not app_ini.exists():
        print("app.ini: NOT FOUND")
        return

    text = app_ini.read_text(encoding="utf-8", errors="ignore")
    matches = [
        line.strip()
        for line in text.splitlines()
        if line.strip().lower().startswith("irsdkenablemem=")
    ]
    if not matches:
        print("irsdkEnableMem: NOT FOUND. Add irsdkEnableMem=1 under [Misc].")
        return
    print(f"irsdkEnableMem: {matches[-1]}")


def main() -> int:
    print("iRacing Radar diagnostics")
    print("=" * 28)

    check_app_ini()

    try:
        response = request.urlopen(SIM_STATUS_URL, timeout=2).read().decode("utf-8", errors="replace")
        print(f"sim status endpoint: OK -> {response.strip()}")
    except error.URLError as exc:
        print(f"sim status endpoint: FAILED -> {exc.reason}")
        print("Make sure the iRacing simulator is inside a live session, not only the UI.")
    except Exception as exc:
        print(f"sim status endpoint: FAILED -> {exc!r}")

    ir = irsdk.IRSDK()
    try:
        started = ir.startup()
        print(f"irsdk startup: {started}")
        print(f"irsdk initialized: {getattr(ir, 'is_initialized', False)}")
        try:
            print(f"irsdk connected: {bool(ir.is_connected)}")
        except Exception as exc:
            print(f"irsdk connected: FAILED -> {exc!r}")

        if started:
            ir.freeze_var_buffer_latest()
            for key in ("PlayerCarIdx", "CarIdxLapDistPct", "CarLeftRight", "SessionTime"):
                try:
                    value = ir[key]
                    if isinstance(value, list):
                        preview = value[:8]
                        print(f"{key}: list len={len(value)} preview={preview}")
                    else:
                        print(f"{key}: {value}")
                except Exception as exc:
                    print(f"{key}: FAILED -> {exc!r}")
    finally:
        ir.shutdown()

    print("=" * 28)
    print("If sim status is not running:1, radar will keep waiting for an iRacing session.")
    print("If startup is False, check Documents\\iRacing\\app.ini has irsdkEnableMem=1, then restart iRacing.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
