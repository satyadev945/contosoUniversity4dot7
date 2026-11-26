@echo off & setlocal enabledelayedexpansion
rem Deploy ContosoUniversity to AWS EKS.

set "APP_NAME=contosouniversity"
set "NAMESPACE=contosouniversity"
set "K8S_SRC=%~dp0..\kubernetes"

where aws >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: aws CLI is required but not installed.
  exit /b 1
)
where kubectl >nul 2>&1
if !ERRORLEVEL! neq 0 (
  echo ERROR: kubectl is required but not installed.
  exit /b 1
)

set "AWS_REGION="
set /p AWS_REGION="AWS region [us-east-1]: "
if "!AWS_REGION!"=="" set "AWS_REGION=us-east-1"
set "CLUSTER_NAME="
set /p CLUSTER_NAME="EKS cluster name: "
if "!CLUSTER_NAME!"=="" (
  echo ERROR: cluster name is required.
  exit /b 1
)
set "IMAGE_URI="
set /p IMAGE_URI="Docker image URI (registry/repo:tag): "
if "!IMAGE_URI!"=="" (
  echo ERROR: image URI is required.
  exit /b 1
)

echo Application configuration - press Enter to skip optional values
set "DB_HOST="
set /p DB_HOST="Enter value for DB_HOST (or press Enter to skip): "
set "DB_NAME="
set /p DB_NAME="Enter value for DB_NAME (or press Enter to skip) [ContosoUniversity]: "
if "!DB_NAME!"=="" set "DB_NAME=ContosoUniversity"
set "DB_USER="
set /p DB_USER="Enter value for DB_USER (or press Enter to skip): "
set "DB_PASSWORD="
set /p DB_PASSWORD="Enter value for DB_PASSWORD (or press Enter to skip, input is visible): "

rem Work on a temporary copy so the template manifests stay untouched
set "WORK_DIR=%TEMP%\cu_k8s_%RANDOM%"
mkdir "!WORK_DIR!"
copy /y "!K8S_SRC!\*.yaml" "!WORK_DIR!\" >nul
if !ERRORLEVEL! neq 0 (
  echo ERROR: could not copy Kubernetes manifests.
  exit /b 1
)

set "TARGET_DIR=!WORK_DIR!"
powershell -NoProfile -Command "Get-ChildItem -LiteralPath $env:TARGET_DIR -Filter *.yaml | ForEach-Object { $c = Get-Content -LiteralPath $_.FullName -Raw; $c = $c.Replace('{{IMAGE_URI}}',$env:IMAGE_URI).Replace('{{NAMESPACE}}',$env:NAMESPACE).Replace('{{DB_HOST}}',$env:DB_HOST).Replace('{{DB_NAME}}',$env:DB_NAME).Replace('{{DB_USER}}',$env:DB_USER); Set-Content -LiteralPath $_.FullName -Value $c -NoNewline }"
if !ERRORLEVEL! neq 0 (
  echo ERROR: placeholder replacement failed.
  exit /b 1
)

echo Configuring kubectl for cluster !CLUSTER_NAME!...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
  echo ERROR: failed to update kubeconfig.
  exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
  echo ERROR: cannot reach the cluster.
  exit /b 1
)

echo Applying manifests...
kubectl apply -f "!WORK_DIR!\namespace.yaml"
if !ERRORLEVEL! neq 0 goto failed

kubectl create secret generic !APP_NAME!-db "--from-literal=DB_PASSWORD=!DB_PASSWORD!" -n !NAMESPACE! --dry-run=client -o yaml | kubectl apply -f -
if !ERRORLEVEL! neq 0 goto failed

kubectl apply -f "!WORK_DIR!\deployment.yaml"
if !ERRORLEVEL! neq 0 goto failed
kubectl apply -f "!WORK_DIR!\service.yaml"
if !ERRORLEVEL! neq 0 goto failed
kubectl apply -f "!WORK_DIR!\ingress.yaml"
if !ERRORLEVEL! neq 0 goto failed

echo Waiting for rollout...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
  echo ERROR: rollout failed. Roll back with:
  echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
  echo Inspect with:
  echo   kubectl describe pods -n !NAMESPACE!
  echo   kubectl logs -l app=!APP_NAME! -n !NAMESPACE! --tail=100
  goto failed
)

kubectl get pods,svc,ingress -n !NAMESPACE!

echo Waiting for the load balancer address - up to 3 minutes...
set "ALB_HOST="
set "TRIES=0"
:waitalb
kubectl get ingress !APP_NAME!-ingress -n !NAMESPACE! -o jsonpath="{.status.loadBalancer.ingress[0].hostname}" > "%TEMP%\cu_alb.txt" 2>nul
set "ALB_HOST="
set /p ALB_HOST=<"%TEMP%\cu_alb.txt"
if not "!ALB_HOST!"=="" goto showurl
set /a TRIES+=1
if !TRIES! geq 18 goto noalb
timeout /t 10 /nobreak >nul
goto waitalb

:showurl
echo Application URL: http://!ALB_HOST!
goto done

:noalb
echo Ingress address not yet assigned. Check later with:
echo   kubectl get ingress -n !NAMESPACE!
echo Requires the AWS Load Balancer Controller to be installed in the cluster.
goto done

:failed
echo Deployment failed.
rmdir /s /q "!WORK_DIR!" >nul 2>&1
exit /b 1

:done
rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
