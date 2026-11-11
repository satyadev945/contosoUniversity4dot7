@echo off
setlocal enabledelayedexpansion

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_DIR=%~dp0..\kubernetes"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: aws CLI is required
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: kubectl is required
  exit /b 1
)

set /p AWS_REGION="AWS region (e.g. us-east-1): "
if "!AWS_REGION!"=="" (
  echo AWS region is required
  exit /b 1
)
set /p CLUSTER_NAME="EKS cluster name: "
if "!CLUSTER_NAME!"=="" (
  echo EKS cluster name is required
  exit /b 1
)
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!IMAGE_URI!"=="" (
  echo Image URI is required
  exit /b 1
)
set /p INGRESS_HOST="Ingress host [contosouniversity.example.com]: "
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contosouniversity.example.com"
set /p SCHOOL_DB_CONNECTION="Enter value for SCHOOL_DB_CONNECTION - SQL Server connection string (or press Enter to skip): "

rem Work on a temp copy so templates keep their placeholders
set "WORK_DIR=%TEMP%\!APP_NAME!-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!K8S_DIR!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
  echo Failed to copy manifests
  exit /b 1
)

powershell -NoProfile -Command "$f=Join-Path $env:WORK_DIR 'deployment.yaml'; (Get-Content $f -Raw).Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE) | Set-Content $f"
if !ERRORLEVEL! neq 0 (
  echo Failed to update deployment.yaml
  exit /b 1
)
powershell -NoProfile -Command "$f=Join-Path $env:WORK_DIR 'ingress.yaml'; (Get-Content $f -Raw).Replace('{{INGRESS_HOST}}',$env:INGRESS_HOST).Replace('{{NAMESPACE}}',$env:NAMESPACE) | Set-Content $f"
if !ERRORLEVEL! neq 0 (
  echo Failed to update ingress.yaml
  exit /b 1
)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo Failed to configure kubectl
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR: cannot reach cluster
  exit /b 1
)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

rem Store connection string in a Secret via a temporary env file
powershell -NoProfile -Command "$f=Join-Path $env:WORK_DIR 'secret.env'; [IO.File]::WriteAllText($f,'SCHOOL_DB_CONNECTION='+$env:SCHOOL_DB_CONNECTION)"
kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! --from-env-file="!WORK_DIR!\secret.env" --dry-run=client -o yaml > "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 (
  echo Failed to render secret
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
del /q "!WORK_DIR!\secret.env" "!WORK_DIR!\secret.yaml" >nul 2>&1

kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 exit /b 1
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: rollout failed. To roll back run:
  echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo.
echo Ingress address - may take a minute to appear:
kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath="{.status.loadBalancer.ingress[0].hostname}"
echo.
echo Rollback if needed: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
rd /s /q "!WORK_DIR!" >nul 2>&1
endlocal
