@echo off
setlocal enabledelayedexpansion

set "PROJECT_NAME=ContosoUniversity"
set "SCRIPT_DIR=%~dp0"
set "DOCKERFILE=%SCRIPT_DIR%..\Dockerfile"
set "BUILD_CONTEXT=%SCRIPT_DIR%..\.."
set "TMPFILE=%TEMP%\contoso_sanitize_%RANDOM%.txt"

echo === ContosoUniversity - Build and Push ===

rem Sanitize image name (lowercase, hyphenate, trim hyphens)
set "RAW_NAME=!PROJECT_NAME!"
powershell -NoProfile -Command "$s=$env:RAW_NAME.ToLower(); $s=[regex]::Replace($s,'[^a-z0-9]+','-'); $s.Trim('-')" > "!TMPFILE!"
set /p IMAGE_NAME=<"!TMPFILE!"

set /p RAW_TAG="Enter image tag [latest]: "
if "!RAW_TAG!"=="" set "RAW_TAG=latest"
powershell -NoProfile -Command "$s=$env:RAW_TAG.ToLower(); $s=[regex]::Replace($s,'[^a-z0-9._]+','-'); $s.Trim('-')" > "!TMPFILE!"
set /p IMAGE_TAG=<"!TMPFILE!"
del "!TMPFILE!" >nul 2>&1
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"

echo Select registry type:
echo   1. Azure Container Registry (ACR)
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" (
    set /p ACR_NAME="Enter ACR name without .azurecr.io: "
    if "!ACR_NAME!"=="" (
        echo ERROR: ACR name is required
        exit /b 1
    )
    set "FULL_IMAGE_NAME=!ACR_NAME!.azurecr.io/!IMAGE_NAME!:!IMAGE_TAG!"
    echo Logging in to ACR...
    az acr login --name !ACR_NAME!
    if !ERRORLEVEL! neq 0 (
        echo ACR login failed
        exit /b 1
    )
) else if "!REGISTRY_CHOICE!"=="2" (
    set /p DOCKER_USERNAME="Enter Docker Hub username: "
    set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
    if "!DOCKER_USERNAME!"=="" (
        echo ERROR: Docker Hub username is required
        exit /b 1
    )
    set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
    echo Logging in to Docker Hub...
    echo !DOCKER_PASSWORD!| docker login --username !DOCKER_USERNAME! --password-stdin
    if !ERRORLEVEL! neq 0 (
        echo Docker Hub login failed
        exit /b 1
    )
) else (
    echo ERROR: invalid registry choice
    exit /b 1
)

echo Building image: !FULL_IMAGE_NAME!
docker build -f "!DOCKERFILE!" -t "!FULL_IMAGE_NAME!" "!BUILD_CONTEXT!"
if !ERRORLEVEL! neq 0 (
    echo Docker build failed
    exit /b 1
)

echo Pushing image: !FULL_IMAGE_NAME!
docker push "!FULL_IMAGE_NAME!"
if !ERRORLEVEL! neq 0 (
    echo Docker push failed
    exit /b 1
)

echo SUCCESS: pushed !FULL_IMAGE_NAME!
echo Use this image URI with scripts\deploy-image.bat
endlocal
