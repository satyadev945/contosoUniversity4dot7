@echo off
setlocal enabledelayedexpansion

REM Deploys ContosoUniversity to Azure AKS.
set "K8S_SRC=%~dp0..\kubernetes"
set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

echo === ContosoUniversity - Deploy to Azure AKS ===

set /p RESOURCE_GROUP="Azure resource group: "
if "!RESOURCE_GROUP!"=="" (
    echo Resource group is required
    exit /b 1
)
set /p CLUSTER_NAME="AKS cluster name: "
if "!CLUSTER_NAME!"=="" (
    echo Cluster name is required
    exit /b 1
)
set /p IMAGE_URI="Docker image URI - registry/image:tag: "
if "!IMAGE_URI!"=="" (
    echo Image URI is required
    exit /b 1
)

echo --- Application configuration - SQL Server - press Enter to skip or use default ---
set /p DB_HOST="Enter value for DB_HOST or press Enter to skip: "
set /p DB_PORT="Enter value for DB_PORT default 1433: "
set /p DB_NAME="Enter value for DB_NAME default ContosoUniversity: "
set /p DB_USER="Enter value for DB_USER default sa: "
set /p DB_PASSWORD="Enter value for DB_PASSWORD or press Enter to skip: "
set /p INGRESS_HOST="Ingress host name default contosouniversity.example.com: "
if "!DB_HOST!"=="" set "DB_HOST=localhost"
if "!DB_PORT!"=="" set "DB_PORT=1433"
if "!DB_NAME!"=="" set "DB_NAME=ContosoUniversity"
if "!DB_USER!"=="" set "DB_USER=sa"
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contosouniversity.example.com"

set "WORK_DIR=%TEMP%\cu_deploy_%RANDOM%"
mkdir "!WORK_DIR!"
copy "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
    echo Failed to copy manifests
    exit /b 1
)

echo Updating manifests
set "DEP_IMAGE_URI=!IMAGE_URI!"
powershell -NoProfile -Command "Get-ChildItem -Path $env:WORK_DIR -Filter *.yaml | ForEach-Object { $c = Get-Content $_.FullName -Raw; $c = $c.Replace('{{IMAGE_URI}}',$env:DEP_IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE).Replace('{{DB_HOST}}',$env:DB_HOST).Replace('{{DB_PORT}}',$env:DB_PORT).Replace('{{DB_NAME}}',$env:DB_NAME).Replace('{{DB_USER}}',$env:DB_USER).Replace('{{INGRESS_HOST}}',$env:INGRESS_HOST); Set-Content -Path $_.FullName -Value $c -NoNewline }"
if !ERRORLEVEL! neq 0 (
    echo Failed to update manifests
    exit /b 1
)

echo Configuring kubectl
az aks get-credentials --resource-group !RESOURCE_GROUP! --name !CLUSTER_NAME! --overwrite-existing
if !ERRORLEVEL! neq 0 (
    echo Failed to get AKS credentials
    exit /b 1
)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
    echo Cannot reach cluster
    exit /b 1
)

echo Applying manifests
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
    echo Failed to apply namespace
    exit /b 1
)

kubectl create secret generic contosouniversity-db --namespace !NAMESPACE! --from-literal=DB_PASSWORD="!DB_PASSWORD!" --dry-run=client -o yaml > "!WORK_DIR!\secret.tmp"
kubectl apply -f "!WORK_DIR!\secret.tmp"
if !ERRORLEVEL! neq 0 (
    echo Failed to apply secret
    exit /b 1
)

kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (
    echo Failed to apply deployment
    exit /b 1
)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (
    echo Failed to apply service
    exit /b 1
)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (
    echo Failed to apply ingress
    exit /b 1
)

echo Waiting for rollout
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
    echo Rollout failed. Roll back with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
    exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!

rmdir /s /q "!WORK_DIR!" >nul 2>&1

echo === Deployment complete ===
echo Get the ingress address with: kubectl get ingress -n !NAMESPACE!
echo Use host header !INGRESS_HOST!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
endlocal
