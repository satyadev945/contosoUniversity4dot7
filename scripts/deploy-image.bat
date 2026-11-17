@echo off
setlocal enabledelayedexpansion
REM Deploy ContosoUniversity to AWS EKS. Run from the repository root.

set "APP_NAME=contoso-university"
set "NAMESPACE=contoso-university"

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!AWS_REGION!"=="" goto missing
if "!CLUSTER_NAME!"=="" goto missing
if "!IMAGE_URI!"=="" goto missing

set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): "
if "!CONNECTION_STRING!"=="" (
  echo ERROR the application requires a SQL Server connection string
  exit /b 1
)

echo Configuring kubectl for cluster !CLUSTER_NAME! ...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo ERROR update-kubeconfig failed
  exit /b 1
)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR cannot reach cluster
  exit /b 1
)

set "WORK_DIR=%TEMP%\contoso-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y kubernetes\*.yaml "!WORK_DIR!\" >nul
powershell -NoProfile -Command "(Get-Content -Raw (Join-Path $env:WORK_DIR 'deployment.yaml')).Replace('{{IMAGE_URI}}', $env:IMAGE_URI) | Set-Content -NoNewline (Join-Path $env:WORK_DIR 'deployment.yaml')"
if !ERRORLEVEL! neq 0 (
  echo ERROR manifest rendering failed
  exit /b 1
)

echo Applying namespace ...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR namespace apply failed
  exit /b 1
)

echo Creating or updating secret ...
kubectl delete secret !APP_NAME!-secrets -n !NAMESPACE! --ignore-not-found
kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=ConnectionStrings__SchoolContext=!CONNECTION_STRING!"
if !ERRORLEVEL! neq 0 (
  echo ERROR secret creation failed
  exit /b 1
)

echo Applying deployment, service, ingress ...
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR deployment apply failed
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR service apply failed
  exit /b 1
)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (
  echo ERROR ingress apply failed
  exit /b 1
)

echo Waiting for rollout ...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR rollout failed. To roll back run:
  echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
for /f "delims=" %%H in ('kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath^="{.status.loadBalancer.ingress[0].hostname}"') do set "INGRESS_HOST=%%H"
if defined INGRESS_HOST (
  echo Application URL: http://!INGRESS_HOST!
) else (
  echo Ingress address not yet assigned. Check later: kubectl get ingress -n !NAMESPACE!
)
rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
exit /b 0

:missing
echo ERROR region, cluster name and image URI are required
exit /b 1
