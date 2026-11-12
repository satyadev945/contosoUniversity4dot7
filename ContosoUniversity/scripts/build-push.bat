@echo off
setlocal enabledelayedexpansion
REM Run from the repository root: ContosoUniversity\scripts\build-push.bat

set "PROJECT_NAME=ContosoUniversity"
powershell -NoProfile -Command "('%PROJECT_NAME%'.ToLower() -replace '[^a-z0-9]+','-').Trim('-')" > "%TEMP%\cu_name.txt"
set /p IMAGE_NAME=<"%TEMP%\cu_name.txt"
del "%TEMP%\cu_name.txt" >nul 2>&1

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW_TAG=!IMAGE_TAG!"
powershell -NoProfile -Command "$t=('!RAW_TAG!'.ToLower() -replace '[^a-z0-9._]+','-').Trim('-'); if ($t -eq '') { $t='latest' }; $t" > "%TEMP%\cu_tag.txt"
set /p IMAGE_TAG=<"%TEMP%\cu_tag.txt"
del "%TEMP%\cu_tag.txt" >nul 2>&1

echo Select registry:
echo   1. AWS ECR
echo   2. Docker Hub
set "REGISTRY_CHOICE="
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" goto ecr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo Invalid choice
exit /b 1

:ecr
set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p AWS_ACCOUNT_ID="AWS account ID: "
set "ECR_REPO="
set /p ECR_REPO="ECR repository name [!IMAGE_NAME!]: "
if "!ECR_REPO!"=="" set "ECR_REPO=!IMAGE_NAME!"
set "REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"
set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"

echo Logging in to ECR...
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
set /p DOCKER_USERNAME="Docker Hub username: "
set /p DOCKER_PASSWORD="Docker Hub password or token: "
set "DOCKER_REPO="
set /p DOCKER_REPO="Repository name [!IMAGE_NAME!]: "
if "!DOCKER_REPO!"=="" set "DOCKER_REPO=!IMAGE_NAME!"
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!DOCKER_REPO!:!IMAGE_TAG!"

echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD!| docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  exit /b 1
)
goto build

:build
echo Building image !FULL_IMAGE_NAME! ...
docker build -f ContosoUniversity\Dockerfile -t !FULL_IMAGE_NAME! .
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

echo Done. Image URI: !FULL_IMAGE_NAME!
endlocal
