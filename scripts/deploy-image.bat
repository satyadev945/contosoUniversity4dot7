@echo off
setlocal enabledelayedexpansion
REM Deploy ContosoUniversity to AWS EKS. Run from the repository root.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

set /p AWS_REGION="Enter AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="Enter EKS cluster name: "
set /p IMAGE_URI="Enter Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" goto missing
if "!CLUSTER_NAME!"=="" goto missing
if "!IMAGE_URI!"=="" goto missing

set /p SCHOOL_DB_CONNECTION="Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): "
if "!SCHOOL_DB_CONNECTION!"=="" (
  echo A database connection string is required
  exit /b 1
)
set /p AUTO_INIT="Enter value for Database__AutoInitialize (true/false, press Enter for true): "
if "!AUTO_INIT!"=="" set "AUTO_INIT=true"

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

set "WORK_DIR=%TEMP%\!APP_NAME!-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y kubernetes\*.yaml "!WORK_DIR!\" >nul
powershell -NoProfile -Command "(Get-Content '!WORK_DIR!\deployment.yaml') -replace [regex]::Escape('{{IMAGE_URI}}'), $env:IMAGE_URI -replace [regex]::Escape('{{DATABASE_AUTOINITIALIZE}}'), $env:AUTO_INIT | Set-Content '!WORK_DIR!\deployment.yaml'"
if !ERRORLEVEL! neq 0 (
  echo Failed to render manifests
  exit /b 1
)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
  echo Namespace apply failed
  exit /b 1
)
kubectl create secret generic contosouniversity-secrets -n !NAMESPACE! --from-literal=SchoolContext="!SCHOOL_DB_CONNECTION!" --dry-run=client -o yaml > "!WORK_DIR!\secret.yaml"
kubectl apply -f "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 (
  echo Secret apply failed
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (
  echo Deployment apply failed
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (
  echo Service apply failed
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (
  echo Ingress apply failed
  exit /b 1
)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. To roll back: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Get the application URL with: kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
rmdir /s /q "!WORK_DIR!"
endlocal
exit /b 0

:missing
echo Region, cluster name and image URI are required
exit /b 1
