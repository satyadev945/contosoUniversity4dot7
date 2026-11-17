@echo off
setlocal enabledelayedexpansion

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "SRC_DIR=%~dp0..\kubernetes"
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"

echo === Deploy !APP_NAME! to Azure AKS ===
set /p RESOURCE_GROUP="Enter Azure resource group: "
set /p CLUSTER_NAME="Enter AKS cluster name: "
set /p IMAGE_URI="Enter Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): "
if "!RESOURCE_GROUP!"=="" (
    echo Resource group is required
    exit /b 1
)
if "!CLUSTER_NAME!"=="" (
    echo Cluster name is required
    exit /b 1
)
if "!IMAGE_URI!"=="" (
    echo Image URI is required
    exit /b 1
)

set "SCHOOL_CONNECTION_STRING="
set "APPINSIGHTS_CS="
set /p SCHOOL_CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): "
set /p APPINSIGHTS_CS="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): "

mkdir "!WORK_DIR!"
copy /y "!SRC_DIR!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
    echo Failed to copy manifests
    exit /b 1
)

REM Values are passed through environment variables to avoid quoting problems
set "CU_IMAGE_URI=!IMAGE_URI!"
set "CU_DB=!SCHOOL_CONNECTION_STRING!"
set "CU_AI=!APPINSIGHTS_CS!"
set "CU_DIR=!WORK_DIR!"
powershell -NoProfile -Command "Get-ChildItem -Path $env:CU_DIR -Filter *.yaml | ForEach-Object { $c = Get-Content -Raw $_.FullName; $c = $c.Replace('{{IMAGE_URI}}',$env:CU_IMAGE_URI).Replace('{{NAMESPACE}}','contosouniversity').Replace('{{SCHOOL_CONNECTION_STRING}}',$env:CU_DB).Replace('{{APPLICATIONINSIGHTS_CONNECTION_STRING}}',$env:CU_AI); [System.IO.File]::WriteAllText($_.FullName,$c) }"
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
    echo Cannot reach the cluster
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
    echo Rollout failed. To roll back run:
    echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
    rmdir /s /q "!WORK_DIR!"
    exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Application is exposed through ingress contosouniversity-ingress (host contosouniversity.example.com).
echo Get the address with: kubectl get ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!

rmdir /s /q "!WORK_DIR!"
endlocal
