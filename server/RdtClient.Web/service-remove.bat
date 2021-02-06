@echo off
set installpath=%~dp0
NET SESSION >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo removing firewall rules...
    netsh.exe advfirewall firewall remove rule name="RealDebridClient" > nul
    echo removing service...   
    net stop RealDebridClient
    timeout /t 5 /nobreak > NUL
    sc delete RealDebridClient
) ELSE (
    echo ######## ########  ########   #######  ########  
    echo ##       ##     ## ##     ## ##     ## ##     ## 
    echo ##       ##     ## ##     ## ##     ## ##     ## 
    echo ######   ########  ########  ##     ## ########  
    echo ##       ##   ##   ##   ##   ##     ## ##   ##   
    echo ##       ##    ##  ##    ##  ##     ## ##    ##  
    echo ######## ##     ## ##     ##  #######  ##     ## 
    echo.
    echo.
    echo ####### ERROR: ADMINISTRATOR PRIVILEGES REQUIRED #########
    echo This script must be run as administrator to work properly!  
    echo If you're seeing this after clicking on a start menu icon, 
    echo then right click on the shortcut and select "Run As Administrator".
    echo ##########################################################
    echo.
    PAUSE
    EXIT /B 1
)
@echo ON