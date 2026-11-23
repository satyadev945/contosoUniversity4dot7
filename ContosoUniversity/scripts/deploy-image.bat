@echo off
setlocal enabledelayedexpansion
REM Deploy ContosoUniversity to AWS EKS

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_SRC=%~dp0..\kubernetes"
set "WORK_DIR=%TEMP%\contosouniversity-k8s-%RANDOM%"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: aws CLI is not installed
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: kubectl is not installed
  exit /b 1
)

set "AWS_REGION="
set "CLUSTER_NAME="
set "IMAGE_URI="
set "INGRESS_HOST="
set "SCHOOL_DB_CONNECTION_STRING="
set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" goto missing
if "!CLUSTER_NAME!"=="" goto missing
if "!IMAGE_URI!"=="" goto missing
set /p INGRESS_HOST="Ingress host name (Enter for contosouniversity.example.com): "
if "!INGRESS_HOST!"=="" set "INGRESS_HOST=contosouniversity.example.com"
set /p SCHOOL_DB_CONNECTION_STRING="Enter value for SCHOOL_DB_CONNECTION_STRING (or press Enter to skip): "

mkdir "!WORK_DIR!"
copy /y "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
  echo ERROR: could not copy manifests
  exit /b 1
)

set "PS_IMAGE_URI=!IMAGE_URI!"
set "PS_INGRESS_HOST=!INGRESS_HOST!"
set "PS_WORK_DIR=!WORK_DIR!"
powershell -NoProfile -Command "$d=$env:PS_WORK_DIR; (Get-Content \"$d\deployment.yaml\") -replace [regex]::Escape('{{IMAGE_URI}}'),$env:PS_IMAGE_URI.Replace('$','$$') | Set-Content \"$d\deployment.yaml\"; (Get-Content \"$d\ingress.yaml\") -replace [regex]::Escape('{{INGRESS_HOST}}'),$env:PS_INGRESS_HOST.Replace('$','$$') | Set-Content \"$d\ingress.yaml\""
if !ERRORLEVEL! neq 0 (
  echo ERROR: placeholder replacement failed
  exit /b 1
)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo ERROR: update-kubeconfig failed
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR: cannot reach the cluster
  exit /b 1
)

echo Applying namespace...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

if not "!SCHOOL_DB_CONNECTION_STRING!"=="" (
  echo Creating/updating secret...
  kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=SCHOOL_DB_CONNECTION_STRING=!SCHOOL_DB_CONNECTION_STRING!" --dry-run=client -o yaml | kubectl apply -f -
  if !ERRORLEVEL! neq 0 (
    echo ERROR: secret creation failed
    exit /b 1
  )
)

echo Applying deployment, service and ingress...
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
echo Get the ALB address with: kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE!
rmdir /s /q "!WORK_DIR!"
endlocal
exit /b 0

:missing
echo Region, cluster name and image URI are required
exit /b 1
