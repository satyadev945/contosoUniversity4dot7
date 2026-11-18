@echo off
setlocal enabledelayedexpansion

rem Build and push the ContosoUniversity image. Script self-locates the repository root.
set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%..\.."

set "PROJECT_NAME=ContosoUniversity"
set "TMPF=%TEMP%\cu_build_%RANDOM%.txt"

powershell -NoProfile -Command "$n='%PROJECT_NAME%'.ToLower() -replace '[^a-z0-9]+','-'; $n.Trim('-')" > "!TMPF!"
set /p IMAGE_NAME=<"!TMPF!"

set "IMAGE_TAG="
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set "IMAGE_TAG=latest"
set "RAW_TAG=!IMAGE_TAG!"
powershell -NoProfile -Command "$t=$env:RAW_TAG.ToLower() -replace '[^a-z0-9._]+','-'; $t=$t.Trim('-'); if ($t -eq '') { $t='latest' }; $t" > "!TMPF!"
set /p IMAGE_TAG=<"!TMPF!"

echo Select registry type:
echo   1. AWS ECR
echo   2. Docker Hub
set "REGISTRY_CHOICE="
set /p REGISTRY_CHOICE="Choice [1/2]: "

if "!REGISTRY_CHOICE!"=="1" goto ecr
if "!REGISTRY_CHOICE!"=="2" goto dockerhub
echo Invalid choice
goto fail

:ecr
set "AWS_REGION="
set /p AWS_REGION="Enter AWS region (e.g. us-east-1): "
if "!AWS_REGION!"=="" (
  echo AWS region is required
  goto fail
)
set "ECR_REPO="
set /p ECR_REPO="Enter ECR repository name [!IMAGE_NAME!]: "
if "!ECR_REPO!"=="" set "ECR_REPO=!IMAGE_NAME!"

echo Resolving AWS account id...
aws sts get-caller-identity --query Account --output text > "!TMPF!"
if !ERRORLEVEL! neq 0 (
  echo Failed to resolve AWS account
  goto fail
)
set /p AWS_ACCOUNT_ID=<"!TMPF!"
set "REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com"
set "FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!"

echo Logging in to ECR...
aws ecr get-login-password --region !AWS_REGION! | docker login --username AWS --password-stdin !REGISTRY_URL!
if !ERRORLEVEL! neq 0 (
  echo ECR login failed
  goto fail
)

echo Ensuring ECR repository exists...
aws ecr describe-repositories --repository-names !ECR_REPO! --region !AWS_REGION! >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo Creating ECR repository...
  aws ecr create-repository --repository-name !ECR_REPO! --region !AWS_REGION!
  if !ERRORLEVEL! neq 0 (
    echo ECR repository creation failed
    goto fail
  )
)
goto build

:dockerhub
set "DOCKER_USERNAME="
set /p DOCKER_USERNAME="Enter Docker Hub username: "
if "!DOCKER_USERNAME!"=="" (
  echo Docker Hub username is required
  goto fail
)
set "DOCKER_PASSWORD="
set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
set "HUB_REPO="
set /p HUB_REPO="Enter repository name [!IMAGE_NAME!]: "
if "!HUB_REPO!"=="" set "HUB_REPO=!IMAGE_NAME!"
set "FULL_IMAGE_NAME=!DOCKER_USERNAME!/!HUB_REPO!:!IMAGE_TAG!"

echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
  echo Docker Hub login failed
  goto fail
)

:build
echo Building image !FULL_IMAGE_NAME! ...
docker build -f ContosoUniversity\Dockerfile -t !FULL_IMAGE_NAME! .
if !ERRORLEVEL! neq 0 (
  echo Docker build failed
  goto fail
)

echo Pushing image !FULL_IMAGE_NAME! ...
docker push !FULL_IMAGE_NAME!
if !ERRORLEVEL! neq 0 (
  echo Docker push failed
  goto fail
)

echo Done. Image pushed to !FULL_IMAGE_NAME!
if exist "!TMPF!" del "!TMPF!"
popd
exit /b 0

:fail
if exist "!TMPF!" del "!TMPF!"
popd
exit /b 1
