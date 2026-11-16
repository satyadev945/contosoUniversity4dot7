@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "K8S_SRC=%SCRIPT_DIR%..\kubernetes"
set "APP_NAME=contosouniversity"

set "RESOURCE_GROUP="
set /p RESOURCE_GROUP="Azure resource group: "
if "%RESOURCE_GROUP%"=="" goto :missing
set "CLUSTER_NAME="
set /p CLUSTER_NAME="AKS cluster name: "
if "%CLUSTER_NAME%"=="" goto :missing
set "IMAGE_URI="
set /p IMAGE_URI="Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): "
if "%IMAGE_URI%"=="" goto :missing
set "NAMESPACE="
set /p NAMESPACE="Kubernetes namespace [contosouniversity]: "
if "%NAMESPACE%"=="" set "NAMESPACE=contosouniversity"
set "INGRESS_HOST="
set /p INGRESS_HOST="Ingress host [contosouniversity.example.com]: "
if "%INGRESS_HOST%"=="" set "INGRESS_HOST=contosouniversity.example.com"
set "DB_HOST="
set /p DB_HOST="Enter value for DB_HOST (or press Enter to skip): "
if "%DB_HOST%"=="" set "DB_HOST=localhost"
set "DB_NAME="
set /p DB_NAME="Enter value for DB_NAME (or press Enter for ContosoUniversity): "
if "%DB_NAME%"=="" set "DB_NAME=ContosoUniversity"
set "DB_USER="
set /p DB_USER="Enter value for DB_USER (or press Enter for sa): "
if "%DB_USER%"=="" set "DB_USER=sa"
set "DB_PASSWORD="
set /p DB_PASSWORD="Enter value for DB_PASSWORD (or press Enter to skip, input is visible): "
set "AI_CONN="
set /p AI_CONN="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): "

set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"
mkdir "%WORK_DIR%"
copy /y "%K8S_SRC%\*.yaml" "%WORK_DIR%\" >nul
if errorlevel 1 goto :fail

powershell -NoProfile -Command "foreach($f in Get-ChildItem -Path $env:WORK_DIR -Filter *.yaml){ $c=Get-Content -Raw $f.FullName; $c=$c.Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE).Replace('{{INGRESS_HOST}}',$env:INGRESS_HOST).Replace('{{DB_HOST}}',$env:DB_HOST).Replace('{{DB_NAME}}',$env:DB_NAME).Replace('{{DB_USER}}',$env:DB_USER); Set-Content -Path $f.FullName -Value $c }"
if errorlevel 1 goto :fail

echo Configuring kubectl for %CLUSTER_NAME% ...
az aks get-credentials --resource-group "%RESOURCE_GROUP%" --name "%CLUSTER_NAME%" --overwrite-existing
if errorlevel 1 goto :fail
kubectl cluster-info
if errorlevel 1 goto :fail

echo Applying manifests ...
kubectl apply -f "%WORK_DIR%\namespace.yaml"
if errorlevel 1 goto :fail
kubectl create secret generic %APP_NAME%-secrets -n %NAMESPACE% --from-literal=DB_PASSWORD="%DB_PASSWORD%" --from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING="%AI_CONN%" --dry-run=client -o yaml | kubectl apply -f -
if errorlevel 1 goto :fail
kubectl apply -f "%WORK_DIR%\deployment.yaml"
if errorlevel 1 goto :fail
kubectl apply -f "%WORK_DIR%\service.yaml"
if errorlevel 1 goto :fail
kubectl apply -f "%WORK_DIR%\ingress.yaml"
if errorlevel 1 goto :fail

echo Waiting for rollout ...
kubectl rollout status deployment/%APP_NAME% -n %NAMESPACE% --timeout=300s
if errorlevel 1 goto :rollout_fail

kubectl get pods,svc,ingress -n %NAMESPACE%
echo Application URL: http://%INGRESS_HOST%
echo Rollback if needed: kubectl rollout undo deployment/%APP_NAME% -n %NAMESPACE%
rmdir /s /q "%WORK_DIR%" >nul 2>&1
endlocal
exit /b 0

:missing
echo Resource group, cluster name and image URI are required.
exit /b 1

:rollout_fail
echo Rollout failed. Roll back with: kubectl rollout undo deployment/%APP_NAME% -n %NAMESPACE%
exit /b 1

:fail
echo Deployment failed.
exit /b 1
