@echo off & setlocal enabledelayedexpansion

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_SRC=%~dp0..\kubernetes"

if not exist "!K8S_SRC!" (
  echo ERROR: kubernetes manifests not found at !K8S_SRC!
  exit /b 1
)

set /p RESOURCE_GROUP="Enter Azure resource group: "
set /p CLUSTER_NAME="Enter AKS cluster name: "
set /p IMAGE_URI="Enter Docker image URI (registry/repo:tag): "
if "!RESOURCE_GROUP!"=="" goto missing
if "!CLUSTER_NAME!"=="" goto missing
if "!IMAGE_URI!"=="" goto missing

set /p DB_HOST="Enter value for DB_HOST (SQL Server host) (or press Enter to skip): "
set /p DB_PORT="Enter value for DB_PORT [1433]: "
set /p DB_NAME="Enter value for DB_NAME [ContosoUniversity]: "
set /p DB_USER="Enter value for DB_USER (or press Enter to skip): "
set /p DB_PASSWORD="Enter value for DB_PASSWORD (or press Enter to skip): "
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): "
set /p DB_INITIALIZE="Run DbInitializer on startup, DB_INITIALIZE (true/false) [false]: "
set /p INGRESS_HOST="Enter ingress host name [contosouniversity.example.com]: "

if "!DB_PORT!"=="" set "DB_PORT=1433"
if "!DB_NAME!"=="" set "DB_NAME=ContosoUniversity"
if "!DB_INITIALIZE!"=="" set "DB_INITIALIZE=false"
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contosouniversity.example.com"

set "WORK_DIR=%TEMP%\contosouniversity-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /Y "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul

set "PS_IMAGE_URI=!IMAGE_URI!"
set "PS_DB_HOST=!DB_HOST!"
set "PS_DB_PORT=!DB_PORT!"
set "PS_DB_NAME=!DB_NAME!"
set "PS_DB_USER=!DB_USER!"
set "PS_DB_INITIALIZE=!DB_INITIALIZE!"
set "PS_INGRESS_HOST=!INGRESS_HOST!"
set "PS_NAMESPACE=!NAMESPACE!"
set "PS_DIR=!WORK_DIR!"

powershell -NoProfile -Command "Get-ChildItem -Path $env:PS_DIR -Filter *.yaml | ForEach-Object { $c = Get-Content $_.FullName -Raw; $c = $c.Replace('{{IMAGE_URI}}',$env:PS_IMAGE_URI).Replace('{{NAMESPACE}}',$env:PS_NAMESPACE).Replace('{{DB_HOST}}',$env:PS_DB_HOST).Replace('{{DB_PORT}}',$env:PS_DB_PORT).Replace('{{DB_NAME}}',$env:PS_DB_NAME).Replace('{{DB_USER}}',$env:PS_DB_USER).Replace('{{DB_INITIALIZE}}',$env:PS_DB_INITIALIZE).Replace('{{INGRESS_HOST}}',$env:PS_INGRESS_HOST); Set-Content -Path $_.FullName -Value $c -NoNewline }"
if !ERRORLEVEL! neq 0 (
  echo Failed to render manifests
  exit /b 1
)

echo Configuring kubectl for AKS cluster !CLUSTER_NAME!...
call az aks get-credentials --resource-group !RESOURCE_GROUP! --name !CLUSTER_NAME! --overwrite-existing
if !ERRORLEVEL! neq 0 (
  echo Failed to get AKS credentials
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo Cluster connectivity check failed
  exit /b 1
)

echo Applying namespace...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
  echo Namespace apply failed
  exit /b 1
)

echo Creating or updating database secret...
kubectl create secret generic contosouniversity-db -n !NAMESPACE! --from-literal=DB_PASSWORD="!DB_PASSWORD!" --from-literal=CONNECTION_STRING="!CONNECTION_STRING!" --dry-run=client -o yaml > "!WORK_DIR!\secret.tmp"
if !ERRORLEVEL! neq 0 (
  echo Secret creation failed
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\secret.tmp"
if !ERRORLEVEL! neq 0 (
  echo Secret apply failed
  exit /b 1
)

echo Applying deployment, service and ingress...
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
  echo Rollout failed. Roll back with:
  echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  echo Inspect with: kubectl describe pods -n !NAMESPACE!
  rmdir /S /Q "!WORK_DIR!"
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Application host: http://!INGRESS_HOST!
echo Get ingress IP with: kubectl get ingress -n !NAMESPACE!

rmdir /S /Q "!WORK_DIR!"
endlocal
exit /b 0

:missing
echo ERROR: resource group, cluster name and image URI are required
exit /b 1
