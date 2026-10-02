@echo off
title HAENG SUNG MEX-MES 3.0 - SERVER (PORT 9999)
color 0A

echo =======================================================================
echo          HAENG SUNG ELECTRONICS - SMART FACTORY MES 3.0
echo                 PRODUCTION SERVER LAUNCHER (PORT 9999)
echo =======================================================================
echo.

:: Lay dia chi IPv4 noi bo cua may tinh
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4 Address" /c:"IPv4"') do (
    set IP=%%a
)
if defined IP (
    set IP=%IP: =%
) else (
    set IP=127.0.0.1
)

echo [INFO] Dang khoi tao he thong HAENG SUNG MES 3.0...
echo [INFO] Binding Server tat ca mang (0.0.0.0:9999)
echo.
echo -----------------------------------------------------------------------
echo  DIA CHI TRUY CAP NHO VAO MANG NOI BO (LAN / NETWORK):
echo   - Localhost (May nay):  http://localhost:9999
echo   - Network IP (May khac): http://%IP%:9999
echo -----------------------------------------------------------------------
echo.
echo Nhan Ctrl+C de dung Server bat ky luc nao.
echo.

set ASPNETCORE_URLS=http://*:9999
set ASPNETCORE_ENVIRONMENT=Production

cd /d "%~dp0"

if exist "MMES.exe" (
    MMES.exe
) else (
    echo [ERROR] Khong tim thay file MMES.exe!
    pause
)
