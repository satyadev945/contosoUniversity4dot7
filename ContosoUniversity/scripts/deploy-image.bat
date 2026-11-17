@echo off
setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to AWS EKS. Run from the ContosoUniversity project directory.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_DIR=%~dp0..\kubernetes"

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" exit /b 1
if "!CLUSTER_NAME!"=="" exit /b 1
if "!IMAGE_URI!"=="" exit /b 1

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo Failed to configure kubectl
  exit /b 1
)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo Cannot reach cluster
  exit /b 1
)

echo Applying namespace...
kubectl apply -f "!K8S_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

set "DB_CONN="
set /p DB_CONN="Enter value for ConnectionStrings__SchoolContext (or press Enter to keep existing secret): "
if not "!DB_CONN!"=="" (
  kubectl create secret generic contosouniversity-db -n !NAMESPACE! --from-literal="ConnectionStrings__SchoolContext=!DB_CONN!" --dry-run=client -o yaml > "%TEMP%\cu-secret.yaml"
  if !ERRORLEVEL! neq 0 (
    echo Failed to render secret
    exit /b 1
  )
  kubectl apply -f "%TEMP%\cu-secret.yaml"
  if !ERRORLEVEL! neq 0 (
    echo Failed to apply secret
    exit /b 1
  )
  del "%TEMP%\cu-secret.yaml"
) else (
  kubectl get secret contosouniversity-db -n !NAMESPACE! >nul 2>&1
  if !ERRORLEVEL! neq 0 (
    echo Secret contosouniversity-db does not exist and no connection string was given.
    exit /b 1
  )
)

echo Applying deployment, service and ingress...
powershell -NoProfile -Command "(Get-Content -Raw '!K8S_DIR!\deployment.yaml') -replace '\{\{IMAGE_URI\}\}', $env:IMAGE_URI" | kubectl apply -f -
if !ERRORLEVEL! neq 0 (
  echo Deployment apply failed
  exit /b 1
)
kubectl apply -f "!K8S_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
kubectl apply -f "!K8S_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. Roll back with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo The application URL is the ADDRESS shown for the ingress above, use http://ADDRESS
endlocal
