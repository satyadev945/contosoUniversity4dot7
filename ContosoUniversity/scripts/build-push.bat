@echo off
setlocal enabledelayedexpansion
rem Build and push the ContosoUniversity image. Run from the repository root (AppCsharpMono):
rem   ContosoUniversity\scripts\build-push.bat

set "PROJECT_NAME=ContosoUniversity"
set "DOCKERFILE=ContosoUniversity\Dockerfile"

if not exist "!DOCKERFILE!" (
  echo ERROR: !DOCKERFILE! not found. Run this script from the repository root.
  exit /b 1
)

set "RAW_NAME=!PROJECT_NAME!"
set "TMPFILE=%TEMP%\cu_sanitized.txt"
powershell -NoProfile -Command "$t=$env:RAW_NAME.ToLower() -replace '[^a-z0-9]+','-'; $t=$t.Trim('-'); Set-Content -NoNewline -Path $env:TMPFILE -Value $t"
set /p IMAGE_NAME=<"!TMPFILE!"

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW_TAG=!IMAGE_TAG!"
powershell -NoProfile -Command "$t=$env:RAW_TAG.ToLower() -replace '[^a-z0-9._]+','-'; $t=$t.Trim('-','.'); if ($t -eq '') { $t='latest' }; Set-Content -NoNewline -Path $env:TMPFILE -Value $t"
set /p IMAGE_TAG=<"!TMPFILE!"
del "!TMPFILE!" >nul 2>&1

echo Select registry:
echo   1. AWS ECR
echo   2. Docker Hub
set "REGISTRY_CHOICE=1"
set /p REGISTRY_CHOICE="Choice [1]: "

if "!REGISTRY_CHOICE!"=="1" goto ecr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo ERROR: invalid registry choice.
exit /b 1

:ecr
set "AWS_REGION=us-east-1"
set /p AWS_REGION="AWS region [us-east-1]: "
set "AWS_ACCOUNT_ID="
set /p AWS_ACCOUNT_ID="AWS account ID: "
if "!AWS_ACCOUNT_ID!"=="" (
  echo ERROR: AWS account ID is required.
  exit /b 1
)
set "ECR_REPO=!IMAGE_NAME!"
set /p ECR_REPO="ECR repository name [!IMAGE_NAME!]: "
set "REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"

echo Logging in to Amazon ECR...
aws ecr get-login-password --region !AWS_REGION! | docker login --username AWS --password-stdin !REGISTRY_URL!
if !ERRORLEVEL! neq 0 (
  echo ECR login failed
  exit /b 1
)

echo Ensuring ECR repository exists...
aws ecr describe-repositories --repository-names !ECR_REPO! --region !AWS_REGION! >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo Creating ECR repository...
  aws ecr create-repository --repository-name !ECR_REPO! --region !AWS_REGION!
  if !ERRORLEVEL! neq 0 (
    echo ECR repository creation failed
    exit /b 1
  )
)
set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"
goto build

:dockerhub
set "DOCKER_USERNAME="
set /p DOCKER_USERNAME="Docker Hub username: "
set "DOCKER_PASSWORD="
set /p DOCKER_PASSWORD="Docker Hub password or access token: "
set "DOCKER_REPO=!IMAGE_NAME!"
set /p DOCKER_REPO="Repository name [!IMAGE_NAME!]: "

echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!DOCKER_REPO!:!IMAGE_TAG!"
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

echo.
echo Done. Image URI to use with deploy-image.bat: !FULL_IMAGE_NAME!
endlocal
