@echo off
setlocal
rem Deploy ContosoUniversity to Azure AKS.

set "SCRIPT_DIR=%~dp0"
set "K8S_DIR=%SCRIPT_DIR%..\kubernetes"
set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

where az >nul 2>&1
if errorlevel 1 (
  echo ERROR: Azure CLI - az - is not installed or not in PATH
  exit /b 1
)
where kubectl >nul 2>&1
if errorlevel 1 (
  echo ERROR: kubectl is not installed or not in PATH
  exit /b 1
)

set "RESOURCE_GROUP="
set /p RESOURCE_GROUP="Azure resource group: "
if "%RESOURCE_GROUP%"=="" (
  echo ERROR: resource group is required
  exit /b 1
)
set "CLUSTER_NAME="
set /p CLUSTER_NAME="AKS cluster name: "
if "%CLUSTER_NAME%"=="" (
  echo ERROR: cluster name is required
  exit /b 1
)
set "IMAGE_URI="
set /p IMAGE_URI="Docker image URI, e.g. myacr.azurecr.io/contosouniversity:latest: "
if "%IMAGE_URI%"=="" (
  echo ERROR: image URI is required
  exit /b 1
)
set "INGRESS_HOST="
set /p INGRESS_HOST="Ingress host name [contosouniversity.example.com]: "
if "%INGRESS_HOST%"=="" set "INGRESS_HOST=contosouniversity.example.com"

echo --- Application configuration - SQL Server ---
set "DB_HOST="
set /p DB_HOST="Enter value for DB_HOST or press Enter to skip: "
set "DB_NAME="
set /p DB_NAME="Enter value for DB_NAME or press Enter for ContosoUniversity: "
if "%DB_NAME%"=="" set "DB_NAME=ContosoUniversity"
set "DB_USER="
set /p DB_USER="Enter value for DB_USER or press Enter for sa: "
if "%DB_USER%"=="" set "DB_USER=sa"
set "DB_PASSWORD="
set /p DB_PASSWORD="Enter value for DB_PASSWORD or press Enter to skip: "
set "APPINSIGHTS="
set /p APPINSIGHTS="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING or press Enter to skip: "

rem Work on a temp copy so the templates keep their placeholders
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"
mkdir "%WORK_DIR%"
copy /y "%K8S_DIR%\*.yaml" "%WORK_DIR%\" >nul
if errorlevel 1 (
  echo ERROR: could not copy manifests
  exit /b 1
)

powershell -NoProfile -Command "function y($s){ if($null -eq $s){''}else{ $s.Replace('\','\\').Replace([string][char]34,'\'+[char]34) } }; Get-ChildItem $env:WORK_DIR -Filter *.yaml | ForEach-Object { $c=[IO.File]::ReadAllText($_.FullName); $c=$c.Replace('{{IMAGE_URI}}',(y $env:IMAGE_URI)).Replace('{{INGRESS_HOST}}',(y $env:INGRESS_HOST)).Replace('{{DB_HOST}}',(y $env:DB_HOST)).Replace('{{DB_NAME}}',(y $env:DB_NAME)).Replace('{{DB_USER}}',(y $env:DB_USER)).Replace('{{DB_PASSWORD}}',(y $env:DB_PASSWORD)).Replace('{{APPLICATIONINSIGHTS_CONNECTION_STRING}}',(y $env:APPINSIGHTS)); [IO.File]::WriteAllText($_.FullName,$c) }"
if errorlevel 1 (
  echo ERROR: failed to update manifests
  exit /b 1
)

echo Configuring kubectl for %CLUSTER_NAME% ...
az aks get-credentials --resource-group "%RESOURCE_GROUP%" --name "%CLUSTER_NAME%" --overwrite-existing
if errorlevel 1 (
  echo ERROR: failed to get AKS credentials
  exit /b 1
)

echo Verifying cluster connectivity ...
kubectl cluster-info
if errorlevel 1 (
  echo ERROR: cannot reach the cluster
  exit /b 1
)

echo Applying manifests ...
kubectl apply -f "%WORK_DIR%\namespace.yaml"
if errorlevel 1 goto :fail
kubectl apply -f "%WORK_DIR%\deployment.yaml"
if errorlevel 1 goto :fail
kubectl apply -f "%WORK_DIR%\service.yaml"
if errorlevel 1 goto :fail
kubectl apply -f "%WORK_DIR%\ingress.yaml"
if errorlevel 1 goto :fail

echo Waiting for rollout ...
kubectl rollout status deployment/%APP_NAME% -n %NAMESPACE% --timeout=300s
if errorlevel 1 goto :rolloutfail

kubectl get pods,svc,ingress -n %NAMESPACE%
echo.
echo Application host: http://%INGRESS_HOST%
echo Ingress address : kubectl get ingress %APP_NAME%-ingress -n %NAMESPACE%
echo Rollback if needed: kubectl rollout undo deployment/%APP_NAME% -n %NAMESPACE%
rmdir /s /q "%WORK_DIR%" >nul 2>&1
endlocal
exit /b 0

:rolloutfail
echo ERROR: rollout failed.
echo Inspect : kubectl describe pods -n %NAMESPACE%
echo Logs    : kubectl logs deployment/%APP_NAME% -n %NAMESPACE%
echo Rollback: kubectl rollout undo deployment/%APP_NAME% -n %NAMESPACE%
rmdir /s /q "%WORK_DIR%" >nul 2>&1
exit /b 1

:fail
echo ERROR: kubectl apply failed
rmdir /s /q "%WORK_DIR%" >nul 2>&1
exit /b 1
