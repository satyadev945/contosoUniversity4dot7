@echo off
setlocal enabledelayedexpansion
REM Build and push the ContosoUniversity image. Run from the REPOSITORY ROOT:
REM   ContosoUniversity\scripts\build-push.bat

set "PROJECT_NAME=ContosoUniversity"
set "DOCKERFILE=ContosoUniversity\Dockerfile"
set "SAN_FILE=%TEMP%\cu_sanitized.txt"

if not exist "!DOCKERFILE!" (
  echo ERROR: !DOCKERFILE! not found. Run this script from the repository root.
  exit /b 1
)

set "RAW_VALUE=!PROJECT_NAME!"
call :sanitize
set "IMAGE_NAME=!SANITIZED!"

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW_VALUE=!IMAGE_TAG!"
call :sanitize
set "IMAGE_TAG=!SANITIZED!"
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"

echo Select registry:
echo   1. AWS ECR
echo   2. Docker Hub
set "REGISTRY_CHOICE="
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" goto ecr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo ERROR: Invalid registry choice.
exit /b 1

:ecr
set "AWS_REGION="
set "AWS_ACCOUNT_ID="
set "ECR_REPO="
set /p AWS_REGION="AWS region, for example us-east-1: "
set /p AWS_ACCOUNT_ID="AWS account ID: "
set /p ECR_REPO="ECR repository name [!IMAGE_NAME!]: "
if "!ECR_REPO!"=="" set "ECR_REPO=!IMAGE_NAME!"
if "!AWS_REGION!"=="" (
  echo ERROR: AWS region is required.
  exit /b 1
)
if "!AWS_ACCOUNT_ID!"=="" (
  echo ERROR: AWS account ID is required.
  exit /b 1
)
set "REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"
set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"

echo Logging in to AWS ECR...
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
goto build

:dockerhub
set "DOCKER_USERNAME="
set "DOCKER_PASSWORD="
set /p DOCKER_USERNAME="Docker Hub username: "
set /p DOCKER_PASSWORD="Docker Hub password or access token: "
if "!DOCKER_USERNAME!"=="" (
  echo ERROR: Docker Hub username is required.
  exit /b 1
)
if "!DOCKER_PASSWORD!"=="" (
  echo ERROR: Docker Hub password is required.
  exit /b 1
)
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"

echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
goto build

:build
echo Building image !FULL_IMAGE_NAME!
docker build -f !DOCKERFILE! -t !FULL_IMAGE_NAME! .
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

echo Done. Image pushed: !FULL_IMAGE_NAME!
echo Use this URI when running scripts\deploy-image.bat
endlocal
exit /b 0

:sanitize
powershell -NoProfile -Command "$s=$env:RAW_VALUE.ToLower(); $s=[regex]::Replace($s,'[^a-z0-9]+','-'); $s=$s.Trim('-'); Write-Output $s" > "!SAN_FILE!"
set "SANITIZED="
set /p SANITIZED=<"!SAN_FILE!"
del "!SAN_FILE!" >nul 2>&1
exit /b 0
