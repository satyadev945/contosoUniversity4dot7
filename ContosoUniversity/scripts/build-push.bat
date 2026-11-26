@echo off & setlocal enabledelayedexpansion
rem Build and push the ContosoUniversity image.
rem Run from the repository root (Mono-db):  ContosoUniversity\scripts\build-push.bat

set "PROJECT_NAME=ContosoUniversity"
set "DOCKERFILE=ContosoUniversity\Dockerfile"

if not exist "!DOCKERFILE!" (
  echo ERROR: !DOCKERFILE! not found. Run this script from the repository root.
  exit /b 1
)

set "RAW=!PROJECT_NAME!"
powershell -NoProfile -Command "$s=$env:RAW.ToLower(); $s=[regex]::Replace($s,'[^a-z0-9]+','-').Trim('-'); Write-Output $s" > "%TEMP%\cu_name.txt"
set /p IMAGE_NAME=<"%TEMP%\cu_name.txt"

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW=!IMAGE_TAG!"
powershell -NoProfile -Command "$s=$env:RAW.ToLower(); $s=[regex]::Replace($s,'[^a-z0-9._]+','-').Trim('-'); if ($s -eq '') { $s='latest' }; Write-Output $s" > "%TEMP%\cu_tag.txt"
set /p IMAGE_TAG=<"%TEMP%\cu_tag.txt"

echo Select registry:
echo   1. AWS ECR
echo   2. Docker Hub
set "REGISTRY_CHOICE="
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" goto ecr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo Invalid choice.
exit /b 1

:ecr
set "AWS_REGION="
set /p AWS_REGION="AWS region [us-east-1]: "
if "!AWS_REGION!"=="" set "AWS_REGION=us-east-1"
set "ECR_REPO="
set /p ECR_REPO="ECR repository name [!IMAGE_NAME!]: "
if "!ECR_REPO!"=="" set "ECR_REPO=!IMAGE_NAME!"

echo Resolving AWS account id...
aws sts get-caller-identity --query Account --output text > "%TEMP%\cu_acct.txt"
if !ERRORLEVEL! neq 0 (
  echo Failed to resolve AWS account id. Check AWS credentials.
  exit /b 1
)
set /p AWS_ACCOUNT_ID=<"%TEMP%\cu_acct.txt"
set "REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"
set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"

echo Logging in to ECR !REGISTRY_URL!...
aws ecr get-login-password --region !AWS_REGION! | docker login --username AWS --password-stdin !REGISTRY_URL!
if !ERRORLEVEL! neq 0 (
  echo ECR login failed
  exit /b 1
)

aws ecr describe-repositories --repository-names !ECR_REPO! --region !AWS_REGION! >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo Creating ECR repository...
  aws ecr create-repository --repository-name !ECR_REPO! --region !AWS_REGION!
  if !ERRORLEVEL! neq 0 (
    echo Failed to create ECR repository
    exit /b 1
  )
)
goto build

:dockerhub
set "DOCKER_USERNAME="
set /p DOCKER_USERNAME="Docker Hub username: "
set "DOCKER_PASSWORD="
set /p DOCKER_PASSWORD="Docker Hub password or access token: "
set "RAW=!DOCKER_USERNAME!"
powershell -NoProfile -Command "Write-Output $env:RAW.ToLower()" > "%TEMP%\cu_user.txt"
set /p DOCKER_USERNAME=<"%TEMP%\cu_user.txt"
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!IMAGE_NAME!:!IMAGE_TAG!"
echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
goto build

:build
echo Building image !FULL_IMAGE_NAME!...
docker build -f "!DOCKERFILE!" -t "!FULL_IMAGE_NAME!" .
if !ERRORLEVEL! neq 0 (
  echo Docker build failed
  exit /b 1
)

echo Pushing image !FULL_IMAGE_NAME!...
docker push "!FULL_IMAGE_NAME!"
if !ERRORLEVEL! neq 0 (
  echo Docker push failed
  exit /b 1
)

echo Done. Image pushed: !FULL_IMAGE_NAME!
echo Use this URI with deploy-image.bat
endlocal
