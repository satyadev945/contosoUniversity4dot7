@echo off
setlocal

set "APP_NAME=contosouniversity"
set "SCRIPT_DIR=%~dp0"
set "K8S_SRC=%SCRIPT_DIR%..\kubernetes"

echo === ContosoUniversity - Deploy to Azure AKS ===

set /p RESOURCE_GROUP="Azure resource group: "
set /p CLUSTER_NAME="AKS cluster name: "
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
set /p NAMESPACE="Kubernetes namespace [contosouniversity]: "
if "%NAMESPACE%"=="" set "NAMESPACE=contosouniversity"

if "%RESOURCE_GROUP%"=="" goto :missing
if "%CLUSTER_NAME%"=="" goto :missing
if "%IMAGE_URI%"=="" goto :missing

set /p CONNECTION_STRING="Enter value for CONNECTION_STRING - ConnectionStrings__SchoolContext (or press Enter to skip): "
set /p APPINSIGHTS_CONNECTION_STRING="Enter value for APPINSIGHTS_CONNECTION_STRING (or press Enter to skip): "

set "WORK_DIR=%TEMP%\contoso-k8s-%RANDOM%"
mkdir "%WORK_DIR%"
xcopy /Y /Q "%K8S_SRC%\*.yaml" "%WORK_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: could not copy manifests
    exit /b 1
)

for %%F in (namespace deployment service ingress) do (
    powershell -NoProfile -Command "$p='%WORK_DIR%\%%F.yaml'; $c=Get-Content -Raw $p; $c=$c.Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE).Replace('{{CONNECTION_STRING}}',$env:CONNECTION_STRING).Replace('{{APPINSIGHTS_CONNECTION_STRING}}',$env:APPINSIGHTS_CONNECTION_STRING); Set-Content -NoNewline -Path $p -Value $c"
    if errorlevel 1 (
        echo ERROR: placeholder replacement failed
        exit /b 1
    )
)

echo Configuring kubectl...
az aks get-credentials --resource-group "%RESOURCE_GROUP%" --name "%CLUSTER_NAME%" --overwrite-existing
if errorlevel 1 (
    echo ERROR: az aks get-credentials failed
    exit /b 1
)
kubectl cluster-info
if errorlevel 1 (
    echo ERROR: cannot reach the cluster
    exit /b 1
)

echo Applying manifests...
kubectl apply -f "%WORK_DIR%\namespace.yaml"
if errorlevel 1 exit /b 1
kubectl apply -f "%WORK_DIR%\deployment.yaml"
if errorlevel 1 exit /b 1
kubectl apply -f "%WORK_DIR%\service.yaml"
if errorlevel 1 exit /b 1
kubectl apply -f "%WORK_DIR%\ingress.yaml"
if errorlevel 1 exit /b 1

echo Waiting for rollout...
kubectl rollout status deployment/%APP_NAME% -n %NAMESPACE% --timeout=300s
if errorlevel 1 (
    echo ERROR: rollout failed. Inspect with: kubectl describe pods -n %NAMESPACE%
    echo Roll back with: kubectl rollout undo deployment/%APP_NAME% -n %NAMESPACE%
    exit /b 1
)

kubectl get pods,svc,ingress -n %NAMESPACE%
echo Ingress address:
kubectl get ingress %APP_NAME%-ingress -n %NAMESPACE% -o jsonpath="{.status.loadBalancer.ingress[0].ip}"
echo.
echo Rollback if needed: kubectl rollout undo deployment/%APP_NAME% -n %NAMESPACE%
rmdir /S /Q "%WORK_DIR%"
endlocal
exit /b 0

:missing
echo ERROR: resource group, cluster name and image URI are required
exit /b 1
