@echo off & setlocal enabledelayedexpansion
rem Build and push the ContosoUniversity image. Run from the repository root.

set "PROJECT_NAME=ContosoUniversity"
set "DOCKERFILE=ContosoUniversity\Dockerfile"

for /f "delims=" %%i in ('powershell -NoProfile -Command "('%PROJECT_NAME%').ToLower() -replace '[^a-z0-9]+','-' -replace '^-+|-+$',''"') do set "IMAGE_NAME=%%i"
if "!IMAGE_NAME!"=="" set "IMAGE_NAME=contosouniversity"

echo === ContosoUniversity build and push ===
echo Select registry:
echo   1. Google Artifact Registry
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Enter choice (1-2): "

set /p RAW_TAG="Enter image tag (default latest): "
set "IMAGE_TAG=latest"
if not "!RAW_TAG!"=="" (
  set "TAGFILE=%TEMP%\cu_tag.txt"
  powershell -NoProfile -Command "$t='!RAW_TAG!'.ToLower() -replace '[^a-z0-9._]+','-' -replace '^-+|-+$',''; if ($t) { $t } else { 'latest' }" > "!TAGFILE!"
  set /p IMAGE_TAG=<"!TAGFILE!"
  del "!TAGFILE!" >nul 2>&1
)

if "!REGISTRY_CHOICE!"=="1" (
  set /p GCP_PROJECT="Enter GCP project ID: "
  set /p GCP_REGION="Enter GCP region (e.g. us-central1): "
  set /p GAR_REPO="Enter Artifact Registry repository name: "
  if "!GCP_PROJECT!"=="" (echo ERROR: GCP project is required & exit /b 1)
  if "!GCP_REGION!"=="" (echo ERROR: GCP region is required & exit /b 1)
  if "!GAR_REPO!"=="" (echo ERROR: Repository name is required & exit /b 1)
  echo Configuring Docker authentication for !GCP_REGION!-docker.pkg.dev
  call gcloud auth configure-docker !GCP_REGION!-docker.pkg.dev --quiet
  if !ERRORLEVEL! neq 0 (echo GAR login failed & exit /b 1)
  set "FULL_IMAGE_NAME=!GCP_REGION!-docker.pkg.dev/!GCP_PROJECT!/!GAR_REPO!/!IMAGE_NAME!:!IMAGE_TAG!"
) else if "!REGISTRY_CHOICE!"=="2" (
  set /p DOCKER_USERNAME="Enter Docker Hub username: "
  set /p DOCKER_PASSWORD="Enter Docker Hub password or token: "
  if "!DOCKER_USERNAME!"=="" (echo ERROR: Username is required & exit /b 1)
  echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
  if !ERRORLEVEL! neq 0 (echo Docker Hub login failed & exit /b 1)
  set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
) else (
  echo ERROR: Invalid choice
  exit /b 1
)

echo Building image !FULL_IMAGE_NAME!
docker build -f !DOCKERFILE! -t !FULL_IMAGE_NAME! .
if !ERRORLEVEL! neq 0 (echo Docker build failed & exit /b 1)

echo Pushing image !FULL_IMAGE_NAME!
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (echo Docker push failed & exit /b 1)

echo SUCCESS: Image pushed to !FULL_IMAGE_NAME!
echo Use this URI with scripts\deploy-image.bat
endlocal
