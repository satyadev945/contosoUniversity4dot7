@echo off & setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "K8S_SRC=%SCRIPT_DIR%..\kubernetes"
set "APP_NAME=contosouniversity"

set /p AWS_REGION="AWS region (e.g. us-east-1): "
set /p CLUSTER_NAME="EKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
set /p NAMESPACE="Kubernetes namespace [contosouniversity]: "
if "!NAMESPACE!"=="" set "NAMESPACE=contosouniversity"
set /p ASPNETCORE_ENVIRONMENT="ASPNETCORE_ENVIRONMENT [Production]: "
if "!ASPNETCORE_ENVIRONMENT!"=="" set "ASPNETCORE_ENVIRONMENT=Production"
set /p CONNECTION_STRING="Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): "

if "!AWS_REGION!"=="" (echo AWS region is required & exit /b 1)
if "!CLUSTER_NAME!"=="" (echo Cluster name is required & exit /b 1)
if "!IMAGE_URI!"=="" (echo Image URI is required & exit /b 1)
if "!CONNECTION_STRING!"=="" (echo Connection string is required & exit /b 1)

set "WORK_DIR=%TEMP%\cu_k8s_%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul

powershell -NoProfile -Command "$b=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($env:CONNECTION_STRING)); Write-Output $b" > "!WORK_DIR!\conn.b64"
set /p CONN_B64=<"!WORK_DIR!\conn.b64"
del "!WORK_DIR!\conn.b64" >nul 2>&1

for %%F in (namespace deployment service ingress) do (
  set "SRC_FILE=!WORK_DIR!\%%F.yaml"
  powershell -NoProfile -Command "$p=$env:SRC_FILE; $c=[IO.File]::ReadAllText($p); $c=$c.Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE).Replace('{{ASPNETCORE_ENVIRONMENT}}',$env:ASPNETCORE_ENVIRONMENT).Replace('{{CONNECTION_STRING_B64}}',$env:CONN_B64); [IO.File]::WriteAllText($p,$c)"
  if !ERRORLEVEL! neq 0 (echo Failed to render %%F.yaml & exit /b 1)
)

echo Configuring kubectl...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (echo update-kubeconfig failed & exit /b 1)
kubectl cluster-info
if !ERRORLEVEL! neq 0 (echo Cannot reach cluster & exit /b 1)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (echo namespace apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (echo deployment apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (echo service apply failed & exit /b 1)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (echo ingress apply failed & exit /b 1)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo Rollout failed. Roll back with: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!
echo.
echo Get the application URL (ALB hostname) with:
echo kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath="{.status.loadBalancer.ingress[0].hostname}"

rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
