@echo off
REM Обход ExecutionPolicy для .ps1 без изменения политики системы.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0apply-docker-migrations.ps1" %*
