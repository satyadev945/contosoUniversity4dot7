@echo off & setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to GKE. Manifests are located relative to this script.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_DIR=%~dp0..\kubernetes"
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"

set /p GCP_PROJECT="GCP project ID: "
set /p GCP_ZONE="GKE cluster zone (or region): "
set /p CLUSTER_NAME="GKE cluster name: "
set /p IMAGE_URI="Docker image URI (registry/path:tag): "
set /p SCHOOL_DB_CONNECTION="Enter value for SCHOOL_DB_CONNECTION (SQL Server connection string): "

if "!GCP_PROJECT!"=="" (echo ERROR: GCP project ID is required & exit /b 1)
if "!GCP_ZONE!"=="" (echo ERROR: zone is required & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo ERROR: cluster name is required & exit /b 1)
if "!IMAGE_URI!"=="" (echo ERROR: image URI is required & exit /b 1)
if "!SCHOOL_DB_CONNECTION!"=="" (echo ERROR: SCHOOL_DB_CONNECTION is required & exit /b 1)

mkdir "!WORK_DIR!"
copy /y "!K8S_DIR!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (echo ERROR: could not copy manifests & exit /b 1)

powershell -NoProfile -Command "$f='!WORK_DIR!\deployment.yaml'; (Get-Content $f -Raw).Replace('{{IMAGE_URI}}','!IMAGE_URI!').Replace('{{NAMESPACE}}','!NAMESPACE!') | Set-Content $f"
if !ERRORLEVEL! neq 0 (echo ERROR: manifest update failed & exit /b 1)

echo Configuring kubectl...
call gcloud container clusters get-credentials !CLUSTER_NAME! --zone !GCP_ZONE! --project !GCP_PROJECT!
if !ERRORLEVEL! neq 0 (echo ERROR: failed to get cluster credentials & exit /b 1)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (echo ERROR: cannot reach cluster & exit /b 1)

echo Applying namespace...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: namespace apply failed & exit /b 1)

echo Creating/updating secret...
kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=SCHOOL_DB_CONNECTION=!SCHOOL_DB_CONNECTION!" --dry-run=client -o yaml > "!WORK_DIR!\secret.tmp"
if !ERRORLEVEL! neq 0 (echo ERROR: secret generation failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\secret.tmp"
if !ERRORLEVEL! neq 0 (echo ERROR: secret apply failed & exit /b 1)
del "!WORK_DIR!\secret.tmp" >nul 2>&1

echo Applying deployment, service, ingress...
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: deployment apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: service apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: ingress apply failed & exit /b 1)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: rollout failed. To roll back run:
  echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo.
echo Get the application IP with:
echo   kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!

rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
