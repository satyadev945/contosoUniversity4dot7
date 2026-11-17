@echo off
setlocal enabledelayedexpansion

set "PROJECT_NAME=ContosoUniversity"
set "TMPFILE=%TEMP%\cu_name_%RANDOM%.txt"

powershell -NoProfile -Command "('%PROJECT_NAME%').ToLower() -replace '[^a-z0-9]+','-' -replace '^-+','' -replace '-+$',''" > "!TMPFILE!"
set /p IMAGE_NAME=<"!TMPFILE!"

echo === Build and push !PROJECT_NAME! image ===
echo Select registry type:
echo   1. Azure Container Registry (ACR)
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Enter choice [1-2]: "
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"

powershell -NoProfile -Command "('!IMAGE_TAG!').ToLower() -replace '[^a-z0-9._-]+','-' -replace '^-+','' -replace '-+$',''" > "!TMPFILE!"
set /p IMAGE_TAG=<"!TMPFILE!"
del "!TMPFILE!" >nul 2>&1
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"

if "!REGISTRY_CHOICE!"=="1" (
    set /p ACR_NAME="Enter ACR name (without .azurecr.io): "
    if "!ACR_NAME!"=="" (
        echo ACR name is required
        exit /b 1
    )
    echo Logging in to ACR !ACR_NAME!...
    az acr login --name !ACR_NAME!
    if !ERRORLEVEL! neq 0 (
        echo ACR login failed
        exit /b 1
    )
    set "FULL_IMAGE_NAME=!ACR_NAME!.azurecr.io/!IMAGE_NAME!:!IMAGE_TAG!"
) else if "!REGISTRY_CHOICE!"=="2" (
    set /p DOCKER_USERNAME="Enter Docker Hub username: "
    set /p DOCKER_PASSWORD="Enter Docker Hub password or token: "
    if "!DOCKER_USERNAME!"=="" (
        echo Username is required
        exit /b 1
    )
    echo Logging in to Docker Hub...
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

echo Building image !FULL_IMAGE_NAME!...
docker build -f ContosoUniversity\Dockerfile -t !FULL_IMAGE_NAME! .
if !ERRORLEVEL! neq 0 (
    echo Docker build failed
    exit /b 1
)

echo Pushing image !FULL_IMAGE_NAME!...
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (
    echo Docker push failed
    exit /b 1
)

echo Done. Image pushed: !FULL_IMAGE_NAME!
echo Use this URI with scripts\deploy-image.bat
endlocal
