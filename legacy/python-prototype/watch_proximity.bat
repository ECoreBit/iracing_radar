@echo off
cd /d %~dp0
.venv\Scripts\python.exe src\iracing_radar\watch_proximity.py
pause
