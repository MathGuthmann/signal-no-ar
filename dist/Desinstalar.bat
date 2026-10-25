@echo off
chcp 65001 >nul
title Desinstalador do Signal No Ar
echo ===========================================
echo       Desinstalador do Signal No Ar
echo ===========================================
echo.
echo Tem certeza de que deseja remover o Signal No Ar?
echo.
pause

echo Fechando aplicativo...
taskkill /f /im SignalNoAr.exe >nul 2>&1

echo Removendo inicialização automática...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "SignalNoAr" /f >nul 2>&1

echo Removendo atalhos...
del "%USERPROFILE%\Desktop\Signal No Ar.lnk" >nul 2>&1
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Signal No Ar.lnk" >nul 2>&1

echo Removendo configurações e arquivos...
rd /s /q "%APPDATA%\SignalNoAr" >nul 2>&1
rd /s /q "%LOCALAPPDATA%\SignalNoAr" >nul 2>&1

echo.
echo ===========================================
echo   Signal No Ar foi removido com sucesso!
echo ===========================================
echo.
pause
