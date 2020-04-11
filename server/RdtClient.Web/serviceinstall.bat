echo OFF
set installpath=%~dp0
NET SESSION >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
   echo adding firewall rules...
   netsh.exe advfirewall firewall add rule name="RealDebridClient" dir=in action=allow program="%installpath%RdtClient.Web.exe" enable=yes > nul

   @echo off
   sc query | findstr /C:"SERVICE_NAME: RealDebridClient" 
   IF ERRORLEVEL 1 (
     echo installing service...   
     nssm install RealDebridClient "%installpath%startup.bat"
   ) 
   IF ERRORLEVEL 0 (
     echo service already installed
   )

   echo.>"%installpath%RdtClient.Web.exe":Zone.Identifier

   echo starting service...
   net start "RealDebridClient"
   echo Starting web app, remember, to set your credentials login with any credentials for the first time.
   ping 127.0.0.1 -n 10 > nul
   start "" http://127.0.0.1:6500/
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