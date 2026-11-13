@echo off & setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to AWS EKS. Run from the repository root.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"

set "AWS_REGION="
set /p AWS_REGION="AWS region (e.g. us-east-1): "
if "!AWS_REGION!"=="" (
    echo AWS region is required
    exit /b 1
)
set "CLUSTER_NAME="
set /p CLUSTER_NAME="EKS cluster name: "
if "!CLUSTER_NAME!"=="" (
    echo EKS cluster name is required
    exit /b 1
)
set "IMAGE_URI="
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!IMAGE_URI!"=="" (
    echo Image URI is required
    exit /b 1
)
set "DB_CONNECTION_STRING="
set /p DB_CONNECTION_STRING="Enter value for DB_CONNECTION_STRING (required): "
if "!DB_CONNECTION_STRING!"=="" (
    echo DB_CONNECTION_STRING is required by the application
    exit /b 1
)
set "AI_CONNECTION_STRING="
set /p AI_CONNECTION_STRING="Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): "

rem Work on a temp copy so the template manifests keep their placeholders
set "WORK_DIR=%TEMP%\cu-k8s-%RANDOM%"
mkdir "!WORK_DIR!"
copy /y kubernetes\*.yaml "!WORK_DIR!" >nul
powershell -NoProfile -Command "(Get-Content '!WORK_DIR!\deployment.yaml') -replace '\{\{IMAGE_URI\}\}', '!IMAGE_URI!' | Set-Content '!WORK_DIR!\deployment.yaml'"
if !ERRORLEVEL! neq 0 (
    echo Failed to update deployment manifest
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
    echo Cannot reach the cluster
    exit /b 1
)

echo Applying namespace...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 (
    echo Namespace apply failed
    exit /b 1
)

echo Creating or updating application secret...
kubectl create secret generic !APP_NAME!-secrets -n !NAMESPACE! "--from-literal=DB_CONNECTION_STRING=!DB_CONNECTION_STRING!" "--from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING=!AI_CONNECTION_STRING!" --dry-run=client -o yaml > "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 (
    echo Secret generation failed
    exit /b 1
)
kubectl apply -f "!WORK_DIR!\secret.yaml"
if !ERRORLEVEL! neq 0 (
    echo Secret apply failed
    exit /b 1
)
del /q "!WORK_DIR!\secret.yaml"

echo Applying deployment, service and ingress...
kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 (
    echo Deployment apply failed
    exit /b 1
)
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 (
    echo Service apply failed
    exit /b 1
)
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 (
    echo Ingress apply failed
    exit /b 1
)

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
    echo Rollout failed. Roll back with:
    echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
    exit /b 1
)

kubectl get pods,svc,ingress -n !NAMESPACE!

echo Ingress address (ALB hostname appears under ADDRESS; may take a few minutes):
kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath="{.status.loadBalancer.ingress[0].hostname}"
echo.
rmdir /s /q "!WORK_DIR!"
endlocal
