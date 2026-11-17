@echo off
setlocal enabledelayedexpansion
REM Deploy ContosoUniversity to AWS EKS.

echo === ContosoUniversity - Deploy to AWS EKS ===

set "SCRIPT_DIR=%~dp0"
set "K8S_SRC=%SCRIPT_DIR%..\kubernetes"
set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: aws CLI is required but not installed.
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: kubectl is required but not installed.
  exit /b 1
)

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" (
  echo ERROR: AWS region is required.
  exit /b 1
)
if "!CLUSTER_NAME!"=="" (
  echo ERROR: cluster name is required.
  exit /b 1
)
if "!IMAGE_URI!"=="" (
  echo ERROR: image URI is required.
  exit /b 1
)

set "ASPNETCORE_ENVIRONMENT=Production"
set /p ASPNETCORE_ENVIRONMENT="ASPNETCORE_ENVIRONMENT [Production]: "
if "!ASPNETCORE_ENVIRONMENT!"=="" set "ASPNETCORE_ENVIRONMENT=Production"
set "CONNECTION_STRING="
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, required): "
if "!CONNECTION_STRING!"=="" (
  echo ERROR: connection string is required.
  exit /b 1
)
set "SKIP_DB_INIT=false"
set /p SKIP_DB_INIT="Enter value for SKIP_DB_INIT (true/false) [false]: "
if "!SKIP_DB_INIT!"=="" set "SKIP_DB_INIT=false"

REM Work on a temporary copy so templates keep their placeholders
set "WORK_DIR=%TEMP%\cu_k8s_%RANDOM%"
mkdir "!WORK_DIR!"
xcopy /E /I /Y /Q "!K8S_SRC!" "!WORK_DIR!" >nul
if !ERRORLEVEL! neq 0 (
  echo ERROR: could not copy manifests.
  exit /b 1
)

REM Placeholder replacement via PowerShell using environment variables as values
powershell -NoProfile -Command "$d=$env:WORK_DIR; $f=Join-Path $d 'deployment.yaml'; $c=Get-Content -Raw $f; $conn=$env:CONNECTION_STRING.Replace('\','\\').Replace('\"','\\\"'); $c=$c.Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{ASPNETCORE_ENVIRONMENT}}',$env:ASPNETCORE_ENVIRONMENT).Replace('{{SKIP_DB_INIT}}',$env:SKIP_DB_INIT).Replace('{{CONNECTION_STRING}}',$conn).Replace('{{NAMESPACE}}',$env:NAMESPACE); Set-Content -NoNewline -Path $f -Value $c"
if !ERRORLEVEL! neq 0 (
  echo ERROR: placeholder replacement failed.
  exit /b 1
)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo ERROR: update-kubeconfig failed.
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR: cannot reach cluster.
  exit /b 1
)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 goto fail
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 goto fail
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 goto fail
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 goto fail

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: rollout failed.
  kubectl get pods -n !NAMESPACE!
  echo To inspect: kubectl logs -l app=!APP_NAME! -n !NAMESPACE! --tail=100
  echo To roll back: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  goto cleanup_fail
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo Get the application URL with:
echo   kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
rmdir /S /Q "!WORK_DIR!" >nul 2>&1
endlocal
exit /b 0

:fail
echo ERROR: kubectl apply failed.
:cleanup_fail
rmdir /S /Q "!WORK_DIR!" >nul 2>&1
exit /b 1
