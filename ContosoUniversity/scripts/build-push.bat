@echo off & setlocal enabledelayedexpansion
rem Build and push the ContosoUniversity image. Run from the repository root:
rem   ContosoUniversity\scripts\build-push.bat

set "PROJECT_NAME=ContosoUniversity"
set "DOCKERFILE=ContosoUniversity\Dockerfile"

if not exist "!DOCKERFILE!" (
  echo ERROR: !DOCKERFILE! not found. Run this script from the repository root.
  exit /b 1
)

set "TMP_OUT=%TEMP%\cu_build_push.txt"
set "IMAGE_NAME="
powershell -NoProfile -Command "(($env:PROJECT_NAME).ToLower() -replace '[^a-z0-9]+','-').Trim('-')" > "%TMP_OUT%"
set /p IMAGE_NAME=<"!TMP_OUT!"
if "!IMAGE_NAME!"=="" set "IMAGE_NAME=contosouniversity"

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW_TAG=!IMAGE_TAG!"
powershell -NoProfile -Command "$t=(($env:RAW_TAG).ToLower() -replace '[^a-z0-9._-]+','-').Trim('-'); if([string]::IsNullOrEmpty($t)){'latest'}else{$t}" > "%TMP_OUT%"
set /p IMAGE_TAG=<"!TMP_OUT!"
del "!TMP_OUT!" >nul 2>&1

echo Select registry type:
echo   1. Azure Container Registry (ACR)
echo   2. Docker Hub
set "REGISTRY_CHOICE="
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" goto acr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo ERROR: invalid registry choice
exit /b 1

:acr
set "ACR_NAME="
set /p ACR_NAME="Enter ACR name (without .azurecr.io): "
if "!ACR_NAME!"=="" (
  echo ERROR: ACR name is required
  exit /b 1
)
set "FULL_IMAGE_NAME=!ACR_NAME!.azurecr.io/!IMAGE_NAME!:!IMAGE_TAG!"
echo Logging in to ACR !ACR_NAME! ...
az acr login --name !ACR_NAME!
if !ERRORLEVEL! neq 0 (
  echo ACR login failed
  exit /b 1
)
goto build

:dockerhub
set "DOCKER_USERNAME="
set /p DOCKER_USERNAME="Enter Docker Hub username: "
if "!DOCKER_USERNAME!"=="" (
  echo ERROR: Docker Hub username is required
  exit /b 1
)
set "DOCKER_PASSWORD="
set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
echo Logging in to Docker Hub ...
echo !DOCKER_PASSWORD!| docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
goto build

:build
echo Building image !FULL_IMAGE_NAME! ...
docker build -f "!DOCKERFILE!" -t "!FULL_IMAGE_NAME!" .
if !ERRORLEVEL! neq 0 (
  echo Docker build failed
  exit /b 1
)

echo Pushing image !FULL_IMAGE_NAME! ...
docker push "!FULL_IMAGE_NAME!"
if !ERRORLEVEL! neq 0 (
  echo Docker push failed
  exit /b 1
)

echo SUCCESS: pushed !FULL_IMAGE_NAME!
echo Use this image URI with scripts\deploy-image.bat
endlocal
