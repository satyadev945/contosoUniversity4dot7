@echo off & setlocal enabledelayedexpansion

set "SRC_DIR=%~dp0..\kubernetes"
set "APP_NAME=contosouniversity"

echo === ContosoUniversity: Deploy to Azure AKS ===

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
set /p IMAGE_URI="Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): "
if "!IMAGE_URI!"=="" (
    echo Image URI is required
    exit /b 1
)
set "NAMESPACE="
set /p NAMESPACE="Kubernetes namespace [contosouniversity]: "
if "!NAMESPACE!"=="" set "NAMESPACE=contosouniversity"
set "INGRESS_HOST="
set /p INGRESS_HOST="Ingress host [contosouniversity.example.com]: "
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contosouniversity.example.com"
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (required): "
if "!CONNECTION_STRING!"=="" (
    echo Connection string is required
    exit /b 1
)
set "APPINSIGHTS_CONNECTION_STRING="
set /p APPINSIGHTS_CONNECTION_STRING="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): "

set "WORK_DIR=%TEMP%\contosouniversity-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!SRC_DIR!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
    echo Failed to copy manifests
    exit /b 1
)

set "P_IMAGE_URI=!IMAGE_URI!"
set "P_NAMESPACE=!NAMESPACE!"
set "P_INGRESS_HOST=!INGRESS_HOST!"
set "P_CONN=!CONNECTION_STRING!"
set "P_AI=!APPINSIGHTS_CONNECTION_STRING!"
set "P_DIR=!WORK_DIR!"
powershell -NoProfile -Command "Get-ChildItem -Path $env:P_DIR -Filter *.yaml | ForEach-Object { $c = Get-Content -Raw $_.FullName; $c = $c.Replace('{{IMAGE_URI}}', $env:P_IMAGE_URI).Replace('{{NAMESPACE}}', $env:P_NAMESPACE).Replace('{{INGRESS_HOST}}', $env:P_INGRESS_HOST).Replace('{{CONNECTION_STRING}}', $env:P_CONN.Replace('\','\\')).Replace('{{APPINSIGHTS_CONNECTION_STRING}}', $env:P_AI); Set-Content -NoNewline -Path $_.FullName -Value $c }"
if !ERRORLEVEL! neq 0 (
    echo Failed to update manifests
    exit /b 1
)

echo Configuring kubectl...
az aks get-credentials --resource-group !RESOURCE_GROUP! --name !CLUSTER_NAME! --overwrite-existing
if !ERRORLEVEL! neq 0 (
    echo Failed to get AKS credentials
    exit /b 1
)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
    echo Cannot connect to cluster
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
    echo Rollout failed. Inspect with: kubectl describe pods -n !NAMESPACE!
    echo Rollback with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
    rmdir /s /q "!WORK_DIR!"
    exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Application URL: http://!INGRESS_HOST!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
rmdir /s /q "!WORK_DIR!"
endlocal
