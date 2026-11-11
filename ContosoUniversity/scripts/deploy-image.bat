@echo off
setlocal enabledelayedexpansion

if exist kubernetes (set "K8S_DIR=kubernetes") else if exist ContosoUniversity\kubernetes (set "K8S_DIR=ContosoUniversity\kubernetes") else (
  echo kubernetes directory not found
  exit /b 1
)

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" (echo AWS region is required & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo Cluster name is required & exit /b 1)
if "!IMAGE_URI!"=="" (echo Image URI is required & exit /b 1)

set /p DB_HOST="Enter value for DB_HOST (SQL Server host): "
set "DB_PORT="
set /p DB_PORT="Enter value for DB_PORT [1433]: "
if "!DB_PORT!"=="" set "DB_PORT=1433"
set "DB_NAME="
set /p DB_NAME="Enter value for DB_NAME [ContosoUniversity]: "
if "!DB_NAME!"=="" set "DB_NAME=ContosoUniversity"
set "DB_USER="
set /p DB_USER="Enter value for DB_USER [sa]: "
if "!DB_USER!"=="" set "DB_USER=sa"
set /p DB_PASSWORD="Enter value for DB_PASSWORD: "
if "!DB_HOST!"=="" (echo DB_HOST is required & exit /b 1)

set "WORK_DIR=%TEMP%\contosouniversity-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!K8S_DIR!\*.yaml" "!WORK_DIR!\" >nul

set "P_IMAGE_URI=!IMAGE_URI!"
set "P_DB_HOST=!DB_HOST!"
set "P_DB_PORT=!DB_PORT!"
set "P_DB_NAME=!DB_NAME!"
set "P_DB_USER=!DB_USER!"
set "P_DB_PASSWORD=!DB_PASSWORD!"
powershell -NoProfile -Command "$f='!WORK_DIR!\deployment.yaml'; $c=Get-Content -Raw $f; $c=$c.Replace('{{IMAGE_URI}}',$env:P_IMAGE_URI).Replace('{{DB_HOST}}',$env:P_DB_HOST).Replace('{{DB_PORT}}',$env:P_DB_PORT).Replace('{{DB_NAME}}',$env:P_DB_NAME).Replace('{{DB_USER}}',$env:P_DB_USER).Replace('{{DB_PASSWORD}}',$env:P_DB_PASSWORD); Set-Content -NoNewline -Path $f -Value $c"
if !ERRORLEVEL! neq 0 (
  echo Failed to update manifests
  exit /b 1
)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo Failed to configure kubectl
  exit /b 1
)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo Cannot reach cluster
  exit /b 1
)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. Roll back with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Get the application URL with: kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Rollback: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
rmdir /s /q "!WORK_DIR!"
endlocal
