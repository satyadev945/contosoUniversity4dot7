@echo off & setlocal enabledelayedexpansion

set "PROJECT_NAME=ContosoUniversity"
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "IMAGE_NAME=contosouniversity"

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW_TAG=!IMAGE_TAG!"
powershell -NoProfile -Command "$t=$env:RAW_TAG.ToLower() -replace '[^a-z0-9._]+','-'; $t=$t.Trim('-','.'); if([string]::IsNullOrEmpty($t)){$t='latest'}; Set-Content -Path $env:TEMP\cu_tag.txt -Value $t -NoNewline"
set /p IMAGE_TAG=<"%TEMP%\cu_tag.txt"
del "%TEMP%\cu_tag.txt" >nul 2>&1

echo Select registry type:
echo   1. Azure Container Registry (ACR)
echo   2. Docker Hub
set "REGISTRY_CHOICE="
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" (
  set "ACR_NAME="
  set /p ACR_NAME="Enter ACR name (without .azurecr.io): "
  if "!ACR_NAME!"=="" (
    echo ACR name is required
    exit /b 1
  )
  set "FULL_IMAGE_NAME=!ACR_NAME!.azurecr.io/!IMAGE_NAME!:!IMAGE_TAG!"
  echo Logging in to ACR !ACR_NAME! ...
  az acr login --name !ACR_NAME!
  if !ERRORLEVEL! neq 0 (
    echo ACR login failed
    exit /b 1
  )
) else if "!REGISTRY_CHOICE!"=="2" (
  set "DOCKER_USERNAME="
  set "DOCKER_PASSWORD="
  set /p DOCKER_USERNAME="Enter Docker Hub username: "
  set /p DOCKER_PASSWORD="Enter Docker Hub password/token: "
  if "!DOCKER_USERNAME!"=="" (
    echo Docker Hub username is required
    exit /b 1
  )
  set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
  echo Logging in to Docker Hub ...
  echo !DOCKER_PASSWORD!| docker login --username !DOCKER_USERNAME! --password-stdin
  if !ERRORLEVEL! neq 0 (
    echo Docker login failed
    exit /b 1
  )
) else (
  echo Invalid choice
  exit /b 1
)

echo Building image !FULL_IMAGE_NAME! ...
docker build -f "%REPO_ROOT%\ContosoUniversity\Dockerfile" -t "!FULL_IMAGE_NAME!" "%REPO_ROOT%"
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

echo Done. Image URI: !FULL_IMAGE_NAME!
endlocal
