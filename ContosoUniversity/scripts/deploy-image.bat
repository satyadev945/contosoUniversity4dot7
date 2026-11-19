@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "MODULE_DIR=%%~fI"
set "K8S_SRC=%MODULE_DIR%\kubernetes"
set "APP_NAME=contoso-university"
set "NAMESPACE=contoso-university"

echo === ContosoUniversity - Deploy to Azure AKS ===

where az >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: Azure CLI az is not installed
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: kubectl is not installed
  exit /b 1
)

set /p RESOURCE_GROUP="Enter Azure resource group name: "
if "!RESOURCE_GROUP!"=="" (
  echo ERROR: Resource group is required
  exit /b 1
)
set /p CLUSTER_NAME="Enter AKS cluster name: "
if "!CLUSTER_NAME!"=="" (
  echo ERROR: AKS cluster name is required
  exit /b 1
)
set /p IMAGE_URI="Enter Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): "
if "!IMAGE_URI!"=="" (
  echo ERROR: Image URI is required
  exit /b 1
)
set /p INGRESS_HOST="Enter ingress host name [contoso-university.example.com]: "
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contoso-university.example.com"

echo Application settings - press Enter to skip
set /p SCHOOL_CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): "
set /p AI_CONNECTION_STRING="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): "

set "RENDER_DIR=%TEMP%\contoso_k8s_%RANDOM%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%render-manifests.ps1" -Source "%K8S_SRC%" -Destination "!RENDER_DIR!"
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
  echo Cannot connect to cluster
  exit /b 1
)

echo Applying namespace...
kubectl apply -f "!RENDER_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
  echo Failed to apply namespace
  exit /b 1
)

echo Creating or updating application secret...
kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=ConnectionStrings__SchoolContext=!SCHOOL_CONNECTION_STRING!" "--from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING=!AI_CONNECTION_STRING!" --dry-run=client -o yaml > "!RENDER_DIR!\secret.generated.yml"
if !ERRORLEVEL! neq 0 (
  echo Failed to build secret
  exit /b 1
)
kubectl apply -f "!RENDER_DIR!\secret.generated.yml"
if !ERRORLEVEL! neq 0 (
  echo Failed to apply secret
  exit /b 1
)
del "!RENDER_DIR!\secret.generated.yml" >nul 2>&1

echo Applying deployment, service and ingress...
kubectl apply -f "!RENDER_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (
  echo Failed to apply deployment
  exit /b 1
)
kubectl apply -f "!RENDER_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (
  echo Failed to apply service
  exit /b 1
)
kubectl apply -f "!RENDER_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (
  echo Failed to apply ingress
  exit /b 1
)

echo Waiting for rollout to complete...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: Rollout failed. Inspect with: kubectl describe pods -n !NAMESPACE!
  echo Rollback with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

echo Deployed resources:
kubectl get pods,svc,ingress -n !NAMESPACE!

echo.
echo Application URL: http://!INGRESS_HOST!
echo Check the ingress address with: kubectl get ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!

rmdir /s /q "!RENDER_DIR!" >nul 2>&1
endlocal
