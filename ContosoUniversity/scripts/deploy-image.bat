@echo off
setlocal enabledelayedexpansion

rem Deploy ContosoUniversity to AWS EKS
set "SCRIPT_DIR=%~dp0"
set "K8S_SRC=%SCRIPT_DIR%..\kubernetes"
set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo aws cli is required
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo kubectl is required
  exit /b 1
)

set "AWS_REGION="
set /p AWS_REGION="Enter AWS region (e.g. us-east-1): "
if "!AWS_REGION!"=="" (
  echo AWS region is required
  exit /b 1
)
set "CLUSTER_NAME="
set /p CLUSTER_NAME="Enter EKS cluster name: "
if "!CLUSTER_NAME!"=="" (
  echo EKS cluster name is required
  exit /b 1
)
set "IMAGE_URI="
set /p IMAGE_URI="Enter Docker image URI, registry/repo:tag: "
if "!IMAGE_URI!"=="" (
  echo Image URI is required
  exit /b 1
)
set "INGRESS_HOST="
set /p INGRESS_HOST="Enter ingress host name [contosouniversity.example.com]: "
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contosouniversity.example.com"
set "SCHOOL_DB_CONNECTION="
set /p SCHOOL_DB_CONNECTION="Enter value for SCHOOL_DB_CONNECTION: "
set "APPLICATIONINSIGHTS_CONNECTION_STRING="
set /p APPLICATIONINSIGHTS_CONNECTION_STRING="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING or press Enter to skip: "

rem Work on a temporary copy so templates keep their placeholders
set "WORK_DIR=%TEMP%\cu_k8s_%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
  echo Failed to copy manifests
  exit /b 1
)

set "DEP=!WORK_DIR!\deployment.yaml"
set "ING=!WORK_DIR!\ingress.yaml"
powershell -NoProfile -Command "(Get-Content -Raw $env:DEP).Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{SCHOOL_DB_CONNECTION}}',$env:SCHOOL_DB_CONNECTION).Replace('{{APPLICATIONINSIGHTS_CONNECTION_STRING}}',$env:APPLICATIONINSIGHTS_CONNECTION_STRING) | Set-Content -NoNewline $env:DEP"
if !ERRORLEVEL! neq 0 (
  echo Failed to update deployment manifest
  exit /b 1
)
powershell -NoProfile -Command "(Get-Content -Raw $env:ING).Replace('{{INGRESS_HOST}}',$env:INGRESS_HOST) | Set-Content -NoNewline $env:ING"
if !ERRORLEVEL! neq 0 (
  echo Failed to update ingress manifest
  exit /b 1
)

echo Configuring kubectl for cluster !CLUSTER_NAME!...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo Failed to update kubeconfig
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo Cannot reach the cluster
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
if !ERRORLEVEL! neq 0 goto rollback

kubectl get pods,svc,ingress -n !NAMESPACE!
echo.
echo Get the load balancer address with:
echo   kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
echo Access it using host header !INGRESS_HOST!
rmdir /s /q "!WORK_DIR!"
exit /b 0

:rollback
echo Rollout failed. To roll back run:
echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
echo Inspect with: kubectl describe pods -n !NAMESPACE!
rmdir /s /q "!WORK_DIR!"
exit /b 1

:fail
echo kubectl apply failed
rmdir /s /q "!WORK_DIR!"
exit /b 1
