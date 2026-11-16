@echo off
setlocal enabledelayedexpansion

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
if exist "kubernetes" (set "K8S_DIR=kubernetes") else (set "K8S_DIR=ContosoUniversity\kubernetes")

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (full path with tag): "
set "CONN_STR="
set /p CONN_STR="Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): "

if "!AWS_REGION!"=="" (echo Region is required & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo Cluster name is required & exit /b 1)
if "!IMAGE_URI!"=="" (echo Image URI is required & exit /b 1)

set "WORK_DIR=%TEMP%\!APP_NAME!-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!K8S_DIR!\*.yaml" "!WORK_DIR!\" >nul

set "TARGET_FILE=!WORK_DIR!\deployment.yaml"
powershell -NoProfile -Command "(Get-Content -Raw $env:TARGET_FILE).Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{CONNECTIONSTRINGS__SCHOOLCONTEXT}}',$env:CONN_STR) | Set-Content -NoNewline $env:TARGET_FILE"
if !ERRORLEVEL! neq 0 (
  echo Failed to update manifests
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
  echo Cannot reach cluster
  exit /b 1
)

echo Applying manifests...
for %%F in (namespace deployment service ingress) do (
  kubectl apply -f "!WORK_DIR!\%%F.yaml"
  if !ERRORLEVEL! neq 0 (
    echo Failed to apply %%F.yaml
    exit /b 1
  )
)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. To roll back: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
for /f "delims=" %%H in ('kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath^="{.status.loadBalancer.ingress[0].hostname}"') do set "ALB_HOST=%%H"
if defined ALB_HOST (echo Application URL: http://!ALB_HOST!) else (echo Ingress address not yet assigned)

rmdir /s /q "!WORK_DIR!"
endlocal
