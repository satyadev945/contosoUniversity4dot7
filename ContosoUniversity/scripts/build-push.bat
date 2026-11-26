@echo off & setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\.."
set "IMAGE_NAME=contosouniversity"

echo === ContosoUniversity build and push ===
set /p RAW_TAG="Image tag [latest]: "
if "!RAW_TAG!"=="" set "RAW_TAG=latest"
powershell -NoProfile -Command "$t=$env:RAW_TAG.ToLower(); $t=[regex]::Replace($t,'[^a-z0-9._]+','-').Trim('-'); if ($t -eq '') { $t='latest' }; Write-Output $t" > "%TEMP%\cu_tag.txt"
set /p IMAGE_TAG=<"%TEMP%\cu_tag.txt"
del "%TEMP%\cu_tag.txt" >nul 2>&1
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
echo Using image tag !IMAGE_TAG!

echo Select registry: 1. AWS ECR  2. Docker Hub
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" (
  set /p AWS_REGION="AWS region (e.g. us-east-1): "
  set /p AWS_ACCOUNT_ID="AWS account ID: "
  if "!AWS_REGION!"=="" (echo Region is required & exit /b 1)
  if "!AWS_ACCOUNT_ID!"=="" (echo Account ID is required & exit /b 1)
  set /p ECR_REPO="ECR repository name [!IMAGE_NAME!]: "
  if "!ECR_REPO!"=="" set "ECR_REPO=!IMAGE_NAME!"
  set "REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"
  set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"
  echo Logging in to ECR...
  aws ecr get-login-password --region !AWS_REGION! | docker login --username AWS --password-stdin !REGISTRY_URL!
  if !ERRORLEVEL! neq 0 (echo ECR login failed & exit /b 1)
  aws ecr describe-repositories --repository-names !ECR_REPO! --region !AWS_REGION! >nul 2>&1
  if !ERRORLEVEL! neq 0 (
    echo Creating ECR repository...
    aws ecr create-repository --repository-name !ECR_REPO! --region !AWS_REGION!
    if !ERRORLEVEL! neq 0 (echo ECR repository creation failed & exit /b 1)
  )
) else if "!REGISTRY_CHOICE!"=="2" (
  set /p DOCKER_USERNAME="Docker Hub username: "
  set /p DOCKER_PASSWORD="Docker Hub password or token: "
  if "!DOCKER_USERNAME!"=="" (echo Username is required & exit /b 1)
  set /p DH_REPO="Repository name [!IMAGE_NAME!]: "
  if "!DH_REPO!"=="" set "DH_REPO=!IMAGE_NAME!"
  set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!DH_REPO!:!IMAGE_TAG!"
  echo Logging in to Docker Hub...
  echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
  if !ERRORLEVEL! neq 0 (echo Docker Hub login failed & exit /b 1)
) else (
  echo Invalid registry choice
  exit /b 1
)

echo Building image !FULL_IMAGE_NAME! ...
docker build -f "%REPO_ROOT%\ContosoUniversity\Dockerfile" -t "!FULL_IMAGE_NAME!" "%REPO_ROOT%"
if !ERRORLEVEL! neq 0 (echo Docker build failed & exit /b 1)

echo Pushing image !FULL_IMAGE_NAME! ...
docker push "!FULL_IMAGE_NAME!"
if !ERRORLEVEL! neq 0 (echo Docker push failed & exit /b 1)

echo Done. Image pushed: !FULL_IMAGE_NAME!
echo Use this URI with scripts\deploy-image.bat
endlocal
