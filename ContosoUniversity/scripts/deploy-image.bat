@echo off & setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to AWS EKS.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_DIR=%~dp0..\kubernetes"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (echo ERROR: aws CLI is required. & exit /b 1)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (echo ERROR: kubectl is required. & exit /b 1)

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" (echo ERROR: region is required. & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo ERROR: cluster name is required. & exit /b 1)
if "!IMAGE_URI!"=="" (echo ERROR: image URI is required. & exit /b 1)
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): "
if "!CONNECTION_STRING!"=="" (echo ERROR: connection string is required. & exit /b 1)

set "RENDER_DIR=%TEMP%\cu_k8s_%RANDOM%"
mkdir "!RENDER_DIR!"
copy /y "!K8S_DIR!\*.yaml" "!RENDER_DIR!\" >nul
if !ERRORLEVEL! neq 0 (echo ERROR: cannot copy manifests. & exit /b 1)

powershell -NoProfile -Command "$f=Join-Path $env:RENDER_DIR 'deployment.yaml'; $b64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($env:CONNECTION_STRING)); (Get-Content $f -Raw).Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{CONNECTION_STRING_B64}}',$b64).Replace('{{NAMESPACE}}',$env:NAMESPACE) | Set-Content -NoNewline $f"
if !ERRORLEVEL! neq 0 (echo ERROR: manifest rendering failed. & exit /b 1)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (echo ERROR: update-kubeconfig failed. & exit /b 1)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (echo ERROR: cannot reach the cluster. & exit /b 1)

echo Applying manifests...
kubectl apply -f "!RENDER_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: namespace apply failed. & exit /b 1)
kubectl apply -f "!RENDER_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: deployment apply failed. & exit /b 1)
kubectl apply -f "!RENDER_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: service apply failed. & exit /b 1)
kubectl apply -f "!RENDER_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (echo ERROR: ingress apply failed. & exit /b 1)

rmdir /s /q "!RENDER_DIR!" >nul 2>&1

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: rollout failed. Inspect with: kubectl describe pods -n !NAMESPACE!
  echo Rollback with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo.
echo Get the application URL (ALB hostname) with:
echo   kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
endlocal
