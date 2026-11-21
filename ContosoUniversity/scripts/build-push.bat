@echo off & setlocal enabledelayedexpansion
rem Build and push the ContosoUniversity image. Run from the repository root (ApSdbzMono).

set "PROJECT_NAME=ContosoUniversity"
set "DOCKERFILE=ContosoUniversity\Dockerfile"
set "TMPFILE=%TEMP%\cu_sanitize.txt"

powershell -NoProfile -Command "('%PROJECT_NAME%').ToLower() -replace '[^a-z0-9]+','-' -replace '^-+|-+$',''" > "%TMPFILE%"
set /p IMAGE_NAME=<"%TMPFILE%"

set "RAW_TAG="
set /p RAW_TAG="Enter image tag [latest]: "
if "!RAW_TAG!"=="" set "RAW_TAG=latest"
powershell -NoProfile -Command "('!RAW_TAG!').ToLower() -replace '[^a-z0-9._-]+','-' -replace '^-+|-+$',''" > "%TMPFILE%"
set "IMAGE_TAG="
set /p IMAGE_TAG=<"%TMPFILE%"
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
del "%TMPFILE%" >nul 2>&1

echo Select registry:
echo   1. Google Artifact Registry
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Choice (1 or 2): "

if "!REGISTRY_CHOICE!"=="1" (
  set /p GCP_PROJECT="GCP project ID: "
  set /p GCP_REGION="GCP region (e.g. us-central1): "
  set /p GAR_REPO="Artifact Registry repository name: "
  if "!GCP_PROJECT!"=="" (echo ERROR: GCP project ID is required & exit /b 1)
  if "!GCP_REGION!"=="" (echo ERROR: GCP region is required & exit /b 1)
  if "!GAR_REPO!"=="" (echo ERROR: repository name is required & exit /b 1)
  set "FULL_IMAGE_NAME=!GCP_REGION!-docker.pkg.dev/!GCP_PROJECT!/!GAR_REPO!/!IMAGE_NAME!:!IMAGE_TAG!"
  echo Authenticating Docker with Artifact Registry...
  call gcloud auth configure-docker !GCP_REGION!-docker.pkg.dev --quiet
  if !ERRORLEVEL! neq 0 (echo GAR login failed & exit /b 1)
) else if "!REGISTRY_CHOICE!"=="2" (
  set /p DOCKER_USERNAME="Docker Hub username: "
  set /p DOCKER_PASSWORD="Docker Hub password or access token: "
  if "!DOCKER_USERNAME!"=="" (echo ERROR: username is required & exit /b 1)
  if "!DOCKER_PASSWORD!"=="" (echo ERROR: password is required & exit /b 1)
  set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
  echo Logging in to Docker Hub...
  echo !DOCKER_PASSWORD!| docker login --username !DOCKER_USERNAME! --password-stdin
  if !ERRORLEVEL! neq 0 (echo Docker Hub login failed & exit /b 1)
) else (
  echo ERROR: invalid choice
  exit /b 1
)

echo Building image !FULL_IMAGE_NAME! ...
docker build -f !DOCKERFILE! -t !FULL_IMAGE_NAME! .
if !ERRORLEVEL! neq 0 (echo Docker build failed & exit /b 1)

echo Pushing image !FULL_IMAGE_NAME! ...
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (echo Docker push failed & exit /b 1)

echo SUCCESS: pushed !FULL_IMAGE_NAME!
echo Use this URI with scripts\deploy-image.bat
endlocal
