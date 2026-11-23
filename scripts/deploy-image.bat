@echo off
setlocal enabledelayedexpansion
REM Deploy ContosoUniversity to AWS EKS. Run from the repository root.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

echo === Deploy !APP_NAME! to AWS EKS ===
set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" (echo Region is required & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo Cluster name is required & exit /b 1)
if "!IMAGE_URI!"=="" (echo Image URI is required & exit /b 1)

echo Application configuration (press Enter to skip)
set /p DB_HOST="Enter value for DB_HOST (or press Enter to skip): "
set /p DB_NAME="Enter value for DB_NAME (or press Enter for ContosoUniversity): "
set /p DB_USER="Enter value for DB_USER (or press Enter to skip): "
set /p DB_PASSWORD="Enter value for DB_PASSWORD (or press Enter to skip): "
set /p CONNECTION_STRING="Enter value for CONNECTION_STRING (or press Enter to skip): "
if "!DB_NAME!"=="" set "DB_NAME=ContosoUniversity"

set "WORK_DIR=%TEMP%\!APP_NAME!-k8s"
if exist "!WORK_DIR!" rmdir /s /q "!WORK_DIR!"
mkdir "!WORK_DIR!"
copy /y kubernetes\*.yaml "!WORK_DIR!\" >nul

set "PS_ARGS=-NoProfile -Command"
powershell !PS_ARGS! "$f='!WORK_DIR!\deployment.yaml'; (Get-Content $f -Raw).Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{DB_HOST}}',$env:DB_HOST).Replace('{{DB_NAME}}',$env:DB_NAME).Replace('{{DB_USER}}',$env:DB_USER) | Set-Content $f -NoNewline"
if !ERRORLEVEL! neq 0 (echo Failed to update manifests & exit /b 1)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (echo Failed to configure kubectl & exit /b 1)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (echo Cannot reach cluster & exit /b 1)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (echo Namespace apply failed & exit /b 1)

kubectl create secret generic contosouniversity-secrets -n !NAMESPACE! --from-literal=DB_PASSWORD="!DB_PASSWORD!" --from-literal=CONNECTION_STRING="!CONNECTION_STRING!" --dry-run=client -o yaml > "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 (echo Secret generation failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 (echo Secret apply failed & exit /b 1)
del "!WORK_DIR!\secret.yaml" >nul 2>&1

kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (echo Deployment apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (echo Service apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (echo Ingress apply failed & exit /b 1)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. To roll back run:
  echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Application URL - run: kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! (ADDRESS column)
rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
