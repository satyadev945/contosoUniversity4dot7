@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "MODULE_DIR=%%~fI"
for %%I in ("%MODULE_DIR%\..") do set "REPO_ROOT=%%~fI"
set "DOCKERFILE=%MODULE_DIR%\Dockerfile"

set "IMAGE_NAME=contosouniversity"

echo === ContosoUniversity - Build and Push ===

set /p RAW_TAG="Enter image tag [latest]: "
set "SANITIZE_TMP=%TEMP%\contoso_tag_%RANDOM%.txt"
powershell -NoProfile -Command "$t = ([string]$env:RAW_TAG).ToLower() -replace '[^a-z0-9._-]+','-'; $t = $t.Trim('-'); if ([string]::IsNullOrEmpty($t)) { $t = 'latest' }; Write-Output $t" > "%SANITIZE_TMP%"
set /p IMAGE_TAG=<"%SANITIZE_TMP%"
del "%SANITIZE_TMP%" >nul 2>&1
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"

echo Select registry type:
echo   1. Azure Container Registry (ACR)
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Enter choice [1-2]: "

if "!REGISTRY_CHOICE!"=="1" goto :acr
if "!REGISTRY_CHOICE!"=="2" goto :dockerhub
echo ERROR: Invalid choice
exit /b 1

:acr
set /p ACR_NAME="Enter ACR name (without .azurecr.io): "
if "!ACR_NAME!"=="" (
  echo ERROR: ACR name is required
  exit /b 1
)
set "FULL_IMAGE_NAME=!ACR_NAME!.azurecr.io/!IMAGE_NAME!:!IMAGE_TAG!"
echo Logging in to Azure Container Registry...
az acr login --name !ACR_NAME!
if !ERRORLEVEL! neq 0 (
  echo ACR login failed
  exit /b 1
)
goto :build

:dockerhub
set /p DOCKER_USERNAME="Enter Docker Hub username: "
if "!DOCKER_USERNAME!"=="" (
  echo ERROR: Docker Hub username is required
  exit /b 1
)
set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD!| docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
goto :build

:build
echo Building image: !FULL_IMAGE_NAME!
docker build -f "!DOCKERFILE!" -t !FULL_IMAGE_NAME! "!REPO_ROOT!"
if !ERRORLEVEL! neq 0 (
  echo Docker build failed
  exit /b 1
)

echo Pushing image: !FULL_IMAGE_NAME!
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (
  echo Docker push failed
  exit /b 1
)

echo Done. Image pushed: !FULL_IMAGE_NAME!
echo Use this URI with scripts\deploy-image.bat
endlocal
