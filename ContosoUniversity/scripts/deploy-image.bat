@echo off
setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to AWS EKS.
rem   ContosoUniversity\scripts\deploy-image.bat

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_SRC=%~dp0..\kubernetes"
set "WORK_DIR=%TEMP%\contosouniversity-k8s"

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

set "AWS_REGION=us-east-1"
set /p AWS_REGION="AWS region [us-east-1]: "
set "CLUSTER_NAME="
set /p CLUSTER_NAME="EKS cluster name: "
if "!CLUSTER_NAME!"=="" (
  echo ERROR: EKS cluster name is required.
  exit /b 1
)
set "IMAGE_URI="
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!IMAGE_URI!"=="" (
  echo ERROR: image URI is required.
  exit /b 1
)
set "INGRESS_HOST=contosouniversity.example.com"
set /p INGRESS_HOST="Ingress host name [contosouniversity.example.com]: "
set "CONNECTION_STRING="
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): "

rem Work on a temporary copy so the templates keep their placeholders
if exist "!WORK_DIR!" rmdir /s /q "!WORK_DIR!"
mkdir "!WORK_DIR!"
xcopy "!K8S_SRC!\*.yaml" "!WORK_DIR!\" /y /q >nul
if !ERRORLEVEL! neq 0 (
  echo ERROR: could not copy Kubernetes manifests.
  exit /b 1
)

set "DEPLOY_FILE=!WORK_DIR!\deployment.yaml"
set "INGRESS_FILE=!WORK_DIR!\ingress.yaml"
powershell -NoProfile -Command "(Get-Content -Raw $env:DEPLOY_FILE).Replace('{{IMAGE_URI}}', $env:IMAGE_URI) | Set-Content -NoNewline $env:DEPLOY_FILE"
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to update deployment.yaml
  exit /b 1
)
powershell -NoProfile -Command "(Get-Content -Raw $env:INGRESS_FILE).Replace('{{INGRESS_HOST}}', $env:INGRESS_HOST) | Set-Content -NoNewline $env:INGRESS_FILE"
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to update ingress.yaml
  exit /b 1
)

echo Configuring kubectl for cluster !CLUSTER_NAME!...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to configure kubectl
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR: cannot reach the cluster
  exit /b 1
)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to apply namespace
  exit /b 1
)

if not "!CONNECTION_STRING!"=="" (
  kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=ConnectionStrings__SchoolContext=!CONNECTION_STRING!" --dry-run=client -o yaml | kubectl apply -f -
  if !ERRORLEVEL! neq 0 (
    echo ERROR: failed to create secret
    exit /b 1
  )
)

kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to apply deployment
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to apply service
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to apply ingress
  exit /b 1
)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: rollout failed. Inspect with: kubectl describe pods -n !NAMESPACE!
  echo Roll back with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!

echo.
echo Application address - run until the ADDRESS column is populated:
echo   kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Use http://ALB-ADDRESS with Host header !INGRESS_HOST!
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!

rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
