@echo off
chcp 65001 >nul
title Instalação do Signal No Ar
echo =======================================================
echo          INSTALAÇÃO DO SIGNAL NO AR
echo =======================================================
echo.
echo 1. Instalando arquivos em %LOCALAPPDATA%\SignalNoAr...

taskkill /f /im SignalNoAr.exe >nul 2>&1

if not exist "%LOCALAPPDATA%\SignalNoAr" mkdir "%LOCALAPPDATA%\SignalNoAr"

copy /y "%~dp0SignalNoAr_Portable\SignalNoAr.exe" "%LOCALAPPDATA%\SignalNoAr\SignalNoAr.exe" >nul
copy /y "%~dp0SignalNoAr_Portable\app.ico" "%LOCALAPPDATA%\SignalNoAr\app.ico" >nul

echo 2. Registrando inicialização automática com o Windows...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "SignalNoAr" /t REG_SZ /d "\"%LOCALAPPDATA%\SignalNoAr\SignalNoAr.exe\"" /f >nul

echo 3. Criando atalhos na Área de Trabalho e Menu Iniciar...
powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), 'Signal No Ar.lnk')); $s.TargetPath = [IO.Path]::Combine($env:LOCALAPPDATA, 'SignalNoAr\SignalNoAr.exe'); $s.IconLocation = [IO.Path]::Combine($env:LOCALAPPDATA, 'SignalNoAr\app.ico,0'); $s.Description = 'Signal No Ar - Painel de Controle e Display'; $s.Save()"
powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([IO.Path]::Combine([Environment]::GetFolderPath('Programs'), 'Signal No Ar.lnk')); $s.TargetPath = [IO.Path]::Combine($env:LOCALAPPDATA, 'SignalNoAr\SignalNoAr.exe'); $s.IconLocation = [IO.Path]::Combine($env:LOCALAPPDATA, 'SignalNoAr\app.ico,0'); $s.Description = 'Signal No Ar - Painel de Controle e Display'; $s.Save()"

echo 4. Criando script de desinstalação...
(
echo @echo off
echo chcp 65001 ^>nul
echo echo Desinstalando Signal No Ar...
echo taskkill /f /im SignalNoAr.exe ^>nul 2^>^&1
echo reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "SignalNoAr" /f ^>nul 2^>^&1
echo del "%%USERPROFILE%%\Desktop\Signal No Ar.lnk" ^>nul 2^>^&1
echo del "%%APPDATA%%\Microsoft\Windows\Start Menu\Programs\Signal No Ar.lnk" ^>nul 2^>^&1
echo rd /s /q "%%APPDATA%%\SignalNoAr" ^>nul 2^>^&1
echo rd /s /q "%%LOCALAPPDATA%\SignalNoAr" ^>nul 2^>^&1
echo echo Desinstalado com sucesso!
echo pause
) > "%LOCALAPPDATA%\SignalNoAr\Desinstalar.bat"

echo.
echo =======================================================
echo          INSTALAÇÃO CONCLUÍDA COM SUCESSO!
echo =======================================================
echo.
echo Abrindo o assistente para você escolher o monitor...
echo.
start "" "%LOCALAPPDATA%\SignalNoAr\SignalNoAr.exe" --setup
