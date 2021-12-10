@ECHO OFF

REM BatchGotAdmin; https://stackoverflow.com/a/10052222/12858021
:-------------------------------------
REM  --> Check for permissions
    IF "%PROCESSOR_ARCHITECTURE%" EQU "amd64" (
>nul 2>&1 "%SYSTEMROOT%\SysWOW64\cacls.exe" "%SYSTEMROOT%\SysWOW64\config\system"
) ELSE (
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
)

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    set params= %*
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %params:"=""%", "", "runas", 1 >> "%temp%\getadmin.vbs"

    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"
:--------------------------------------   

@REM https://docs.microsoft.com/en-us/office/dev/add-ins/testing/create-a-network-shared-folder-catalog-for-task-pane-and-content-add-ins

SET swateFolderName=nfdi_manifests
SET swateFolder=%LOCALAPPDATA%\%swateFolderName%

IF EXIST %swateFolder% (
    ECHO Found existing folder for Swate manifests!
) ELSE (
    mkdir %swateFolder%
)

ECHO Created folder for Swate manifest @%swateFolder%.

ECHO Download Swate manifest to folder..

curl.exe --output %swateFolder%/manifest.xml --url https://raw.githubusercontent.com/nfdi4plants/Swate/developer/.assets/assets/manifest.xml

ECHO Share folder with Excel network..

net share swate_manifests=%swateFolder% /grant:%USERNAME%,FULL

ECHO Create registry file for Excel..

@REM Network path always contains computer name as first parameter
(
    ECHO Windows Registry Editor Version 5.00
    ECHO ""
    ECHO [HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\WEF\TrustedCatalogs\{aa2b5cf8-00a8-4d2e-9781-8c00ac234d73}]
    ECHO "Id"="{aa2b5cf8-00a8-4d2e-9781-8c00ac234d73}"
    ECHO "Url"="\\\\%computername%\\%swateFolderName%"
    ECHO "Flags"=dword:00000001 
) > %swateFolder%/TrustNetworkShareCatalog.reg

ECHO Execute registry file for Excel..

%swateFolder%/TrustNetworkShareCatalog.reg

REM https://stackoverflow.com/questions/2048509/how-to-echo-with-different-colors-in-the-windows-command-line

ECHO [32mDone![0m
pause