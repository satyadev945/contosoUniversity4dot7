@echo off
setlocal enabledelayedexpansion

REM Builds and pushes the ContosoUniversity image. Docker build context is the repository root.
cd /d "%~dp0..\.."
if !ERRORLEVEL! neq 0 (
    echo Unable to change to repository root
    exit /b 1
)

set "PROJECT_NAME=ContosoUniversity"
set "TMPF=%TEMP%\cu_sanitize_%RANDOM%.txt"

set "RAW_VALUE=%PROJECT_NAME%"
powershell -NoProfile -Command "($env:RAW_VALUE).ToLower() -replace '[^a-z0-9]+','-' -replace '^-+',''  -replace '-+$',''" > "%TMPF%"
set /p IMAGE_NAME=<"%TMPF%"
del "%TMPF%" >nul 2>&1
if "!IMAGE_NAME!"=="" set "IMAGE_NAME=contosouniversity"

echo === ContosoUniversity - Build and Push ===
echo Select registry type:
echo   1. Azure Container Registry - ACR
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Enter choice 1 or 2: "

set /p IMAGE_TAG="Enter image tag [latest]: "
set "RAW_VALUE=!IMAGE_TAG!"
powershell -NoProfile -Command "($env:RAW_VALUE).ToLower() -replace '[^a-z0-9._]+','-' -replace '^-+',''  -replace '-+$',''" > "%TMPF%"
set "IMAGE_TAG="
set /p IMAGE_TAG=<"%TMPF%"
del "%TMPF%" >nul 2>&1
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"

if "!REGISTRY_CHOICE!"=="1" (
    set /p ACR_NAME="Enter ACR name without .azurecr.io: "
    if "!ACR_NAME!"=="" (
        echo ACR name is required
        exit /b 1
    )
    echo Logging in to ACR !ACR_NAME!
    az acr login --name !ACR_NAME!
    if !ERRORLEVEL! neq 0 (
        echo ACR login failed
        exit /b 1
    )
    set "FULL_IMAGE_NAME=!ACR_NAME!.azurecr.io/!IMAGE_NAME!:!IMAGE_TAG!"
) else if "!REGISTRY_CHOICE!"=="2" (
    set /p DOCKER_USERNAME="Enter Docker Hub username: "
    set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
    if "!DOCKER_USERNAME!"=="" (
        echo Docker Hub username is required
        exit /b 1
    )
    echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
    if !ERRORLEVEL! neq 0 (
        echo Docker Hub login failed
        exit /b 1
    )
    set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
) else (
    echo Invalid choice
    exit /b 1
)

echo Building image !FULL_IMAGE_NAME!
docker build -f ContosoUniversity\Dockerfile -t !FULL_IMAGE_NAME! .
if !ERRORLEVEL! neq 0 (
    echo Docker build failed
    exit /b 1
)

echo Pushing image !FULL_IMAGE_NAME!
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (
    echo Docker push failed
    exit /b 1
)

echo === Done ===
echo Image pushed: !FULL_IMAGE_NAME!
echo Use this URI with scripts\deploy-image.bat
endlocal
