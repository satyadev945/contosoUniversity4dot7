@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "K8S_SRC=%SCRIPT_DIR%..\kubernetes"
set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"

set /p AWS_REGION="AWS region (e.g. us-east-1): "
if "!AWS_REGION!"=="" (
  echo AWS region is required
  exit /b 1
)
set /p CLUSTER_NAME="EKS cluster name: "
if "!CLUSTER_NAME!"=="" (
  echo Cluster name is required
  exit /b 1
)
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!IMAGE_URI!"=="" (
  echo Image URI is required
  exit /b 1
)
set "CONNECTION_STRING="
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): "
if "!CONNECTION_STRING!"=="" (
  set "CONNECTION_STRING=Server=localhost,1433;Database=ContosoUniversity;User Id=sa;Password=changeme;TrustServerCertificate=True"
  echo WARNING: using default placeholder connection string.
)

mkdir "!WORK_DIR!"
copy "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
  echo Failed to copy manifests
  exit /b 1
)

set "CU_IMAGE_URI=!IMAGE_URI!"
set "CU_CONN=!CONNECTION_STRING!"
set "CU_FILE=!WORK_DIR!\deployment.yaml"
powershell -NoProfile -Command "$f=$env:CU_FILE; $c=Get-Content -Raw $f; $conn=$env:CU_CONN.Replace('\','\\').Replace('\"','\\\"'); $c=$c.Replace('{{IMAGE_URI}}',$env:CU_IMAGE_URI).Replace('{{CONNECTION_STRING}}',$conn).Replace('{{NAMESPACE}}','%NAMESPACE%'); Set-Content -NoNewline -Path $f -Value $c"
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
echo Get the application URL with:
echo   kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!

rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
