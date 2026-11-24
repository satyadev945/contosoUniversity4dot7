@echo off
setlocal enabledelayedexpansion
REM Deploy ContosoUniversity to AWS EKS.
REM   ContosoUniversity\scripts\deploy-image.bat

set "APP_NAME=contosouniversity"
set "K8S_DIR=%~dp0..\kubernetes"
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: aws CLI is not installed or not in PATH.
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: kubectl is not installed or not in PATH.
  exit /b 1
)

set "AWS_REGION="
set "CLUSTER_NAME="
set "IMAGE_URI="
set "NAMESPACE="
set "DB_CONNECTION="
set /p AWS_REGION="AWS region, for example us-east-1: "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI, registry/repo:tag: "
set /p NAMESPACE="Kubernetes namespace [!APP_NAME!]: "
if "!NAMESPACE!"=="" set "NAMESPACE=!APP_NAME!"
set /p DB_CONNECTION="Enter value for ConnectionStrings__SchoolContext, SQL Server connection string, or press Enter to skip: "

if "!AWS_REGION!"=="" (
  echo ERROR: AWS region is required.
  exit /b 1
)
if "!CLUSTER_NAME!"=="" (
  echo ERROR: EKS cluster name is required.
  exit /b 1
)
if "!IMAGE_URI!"=="" (
  echo ERROR: Image URI is required.
  exit /b 1
)

mkdir "!WORK_DIR!"
copy /Y "!K8S_DIR!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
  echo ERROR: could not copy Kubernetes manifests.
  exit /b 1
)

powershell -NoProfile -Command "Get-ChildItem $env:WORK_DIR -Filter *.yaml | ForEach-Object { $c=[IO.File]::ReadAllText($_.FullName); $c=$c.Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE); [IO.File]::WriteAllText($_.FullName,$c) }"
if !ERRORLEVEL! neq 0 (
  echo ERROR: placeholder replacement failed.
  exit /b 1
)

echo Configuring kubectl for cluster !CLUSTER_NAME!...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo ERROR: update-kubeconfig failed.
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR: cannot reach the cluster.
  exit /b 1
)

echo Applying namespace...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 exit /b 1

if not "!DB_CONNECTION!"=="" (
  echo Creating or updating secret !APP_NAME!-secrets...
  kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=ConnectionStrings__SchoolContext=!DB_CONNECTION!" --dry-run=client -o yaml | kubectl apply -f -
  if !ERRORLEVEL! neq 0 (
    echo ERROR: secret creation failed.
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

echo Application URL, available once the ALB is provisioned:
kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath="http://{.status.loadBalancer.ingress[0].hostname}"
echo.

rmdir /S /Q "!WORK_DIR!" >nul 2>&1
endlocal
exit /b 0
