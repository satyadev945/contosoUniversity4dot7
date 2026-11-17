@echo off & setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to GCP GKE. Run from the repository root.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"

echo === ContosoUniversity GKE deployment ===
set /p GCP_PROJECT="Enter GCP project ID: "
set /p GCP_ZONE="Enter GCP zone (e.g. us-central1-a): "
set /p CLUSTER_NAME="Enter GKE cluster name: "
set /p IMAGE_URI="Enter Docker image URI (full path with tag): "

if "!GCP_PROJECT!"=="" (echo ERROR: GCP project is required & exit /b 1)
if "!GCP_ZONE!"=="" (echo ERROR: GCP zone is required & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo ERROR: Cluster name is required & exit /b 1)
if "!IMAGE_URI!"=="" (echo ERROR: Image URI is required & exit /b 1)

echo Enter value for SCHOOL_CONNECTION_STRING (SQL Server connection string, or press Enter to skip)
set "SCHOOL_CONNECTION_STRING="
set /p SCHOOL_CONNECTION_STRING="> "
if "!SCHOOL_CONNECTION_STRING!"=="" set "SCHOOL_CONNECTION_STRING=Server=localhost;Database=ContosoUniversity;TrustServerCertificate=True"

mkdir "!WORK_DIR!"
copy /y kubernetes\*.yaml "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (echo Failed to copy manifests & exit /b 1)

set "CU_IMAGE_URI=!IMAGE_URI!"
set "CU_CONN=!SCHOOL_CONNECTION_STRING!"
set "CU_NS=!NAMESPACE!"
set "CU_DIR=!WORK_DIR!"
powershell -NoProfile -Command "Get-ChildItem $env:CU_DIR -Filter *.yaml | ForEach-Object { $c = Get-Content $_.FullName -Raw; $c = $c.Replace('{{IMAGE_URI}}',$env:CU_IMAGE_URI).Replace('{{NAMESPACE}}',$env:CU_NS).Replace('{{SCHOOL_CONNECTION_STRING}}',$env:CU_CONN); Set-Content -Path $_.FullName -Value $c -NoNewline }"
if !ERRORLEVEL! neq 0 (echo Failed to update manifests & exit /b 1)

echo Configuring kubectl...
call gcloud container clusters get-credentials !CLUSTER_NAME! --zone !GCP_ZONE! --project !GCP_PROJECT!
if !ERRORLEVEL! neq 0 (echo Failed to get cluster credentials & exit /b 1)

kubectl cluster-info
if !ERRORLEVEL! neq 0 (echo Cannot reach cluster & exit /b 1)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (echo namespace apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (echo deployment apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (echo service apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (echo ingress apply failed & exit /b 1)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. To roll back run: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Check ingress address: kubectl get ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!

rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
