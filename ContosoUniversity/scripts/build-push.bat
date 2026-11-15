@echo off
setlocal enabledelayedexpansion

rem Build and push the ContosoUniversity image to AWS ECR or Docker Hub.
rem The Docker build context is the ContosoUniversity module directory (parent of this scripts folder).

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "MODULE_DIR=%%~fI"
set "PROJECT_NAME=ContosoUniversity"

rem Sanitize image name: lowercase, non-alphanumerics to hyphen, trim hyphens
set "RAW_NAME=!PROJECT_NAME!"
powershell -NoProfile -Command "$t=$env:RAW_NAME.ToLower(); $t=[regex]::Replace($t,'[^a-z0-9]+','-').Trim('-'); Write-Output $t" > "%TEMP%\cu_name.txt"
set "IMAGE_NAME="
set /p IMAGE_NAME=<"%TEMP%\cu_name.txt"
if "!IMAGE_NAME!"=="" set "IMAGE_NAME=contosouniversity"

echo === ContosoUniversity build and push ===
echo Select registry type:
echo   1. AWS ECR
echo   2. Docker Hub
set /p REGISTRY_CHOICE="Enter choice (1 or 2): "

set /p RAW_TAG="Enter image tag (default: latest): "
powershell -NoProfile -Command "$t=([string]$env:RAW_TAG).ToLower(); $t=[regex]::Replace($t,'[^a-z0-9._-]+','-').Trim('-','.'); if([string]::IsNullOrEmpty($t)){$t='latest'}; Write-Output $t" > "%TEMP%\cu_tag.txt"
set "IMAGE_TAG="
set /p IMAGE_TAG=<"%TEMP%\cu_tag.txt"
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
echo Using image name !IMAGE_NAME! and tag !IMAGE_TAG!

set "BUILD_ARGS="

if "!REGISTRY_CHOICE!"=="1" goto ecr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo Invalid choice. Enter 1 or 2.
exit /b 1

:ecr
set /p AWS_REGION="Enter AWS region (e.g., us-east-1): "
if "!AWS_REGION!"=="" (
  echo AWS region is required
  exit /b 1
)
set "ACCOUNT_ID="
aws sts get-caller-identity --query Account --output text > "%TEMP%\cu_acct.txt"
if !ERRORLEVEL! neq 0 (
  echo Could not determine AWS account id. Check your AWS credentials.
  exit /b 1
)
set /p ACCOUNT_ID=<"%TEMP%\cu_acct.txt"
set "REGISTRY_URL=!ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"
set "ECR_REPO=!IMAGE_NAME!"
set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"

echo Logging in to ECR !REGISTRY_URL! ...
aws ecr get-login-password --region !AWS_REGION! | docker login --username AWS --password-stdin !REGISTRY_URL!
if !ERRORLEVEL! neq 0 (
  echo ECR login failed
  exit /b 1
)

echo Ensuring ECR repository !ECR_REPO! exists ...
aws ecr describe-repositories --repository-names !ECR_REPO! --region !AWS_REGION! >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo Creating ECR repository...
  aws ecr create-repository --repository-name !ECR_REPO! --region !AWS_REGION!
  if !ERRORLEVEL! neq 0 (
    echo ECR repository creation failed
    exit /b 1
  )
)

set "CACHE_PREFIX="
set /p CACHE_PREFIX="ECR pull-through cache prefix for MCR base images (leave empty to use mcr.microsoft.com): "
if not "!CACHE_PREFIX!"=="" set "BUILD_ARGS=--build-arg BASE_REGISTRY=!REGISTRY_URL!/!CACHE_PREFIX!"
goto build

:dockerhub
set /p DOCKER_USERNAME="Enter Docker Hub username: "
set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
if "!DOCKER_USERNAME!"=="" (
  echo Docker Hub username is required
  exit /b 1
)
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
echo Logging in to Docker Hub ...
echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
goto build

:build
echo Building image !FULL_IMAGE_NAME! ...
docker build -f "!MODULE_DIR!\Dockerfile" !BUILD_ARGS! -t !FULL_IMAGE_NAME! "!MODULE_DIR!"
if !ERRORLEVEL! neq 0 (
  echo Docker build failed
  exit /b 1
)

echo Pushing image !FULL_IMAGE_NAME! ...
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (
  echo Docker push failed
  exit /b 1
)

echo.
echo Build and push completed successfully.
echo Image URI: !FULL_IMAGE_NAME!
echo Use this URI when running scripts\deploy-image.bat
endlocal
exit /b 0
