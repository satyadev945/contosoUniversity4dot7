@echo off
setlocal enabledelayedexpansion

rem Deploy ContosoUniversity to AWS ECS Fargate (Windows). Requires AWS CLI and PowerShell.

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ECS_DIR=%%~fI\ecs"
set "PROJECT_NAME=contosouniversity"
set "SERVICE_NAME=%PROJECT_NAME%-service"
set "LOG_GROUP=/ecs/%PROJECT_NAME%"
set "CONTAINER_PORT=8080"
set "HEALTH_PATH=/health"

set /p AWS_REGION="Enter AWS region (e.g., us-east-1): "
set /p CLUSTER_NAME="Enter ECS cluster name (e.g., my-ecs-cluster): "
set /p VPC_ID="Enter VPC ID (e.g., vpc-0abc123def456): "
set /p SUBNET_IDS="Enter Subnet IDs comma-separated, at least two (e.g., subnet-0abc123,subnet-0def456): "
set /p SECURITY_GROUP="Enter Security Group ID (e.g., sg-0abc123def): "
set /p IMAGE_URI="Enter Docker image URI (e.g., 123456789.dkr.ecr.us-east-1.amazonaws.com/contosouniversity:latest): "
set /p DB_SECRET_ARN="Enter Secrets Manager or SSM ARN holding the SQL Server connection string: "
set /p NEED_LB="Do you need a load balancer for this service? (y/n): "

set "SUBNET_1="
set "SUBNET_2="
for /f "tokens=1,2 delims=, " %%a in ("!SUBNET_IDS!") do (
  set "SUBNET_1=%%a"
  set "SUBNET_2=%%b"
)
if "!SUBNET_1!"=="" goto badsubnets
if "!SUBNET_2!"=="" goto badsubnets
if "!AWS_REGION!"=="" goto missing
if "!CLUSTER_NAME!"=="" goto missing
if "!SECURITY_GROUP!"=="" goto missing
if "!IMAGE_URI!"=="" goto missing
if "!DB_SECRET_ARN!"=="" goto missing
set "AWS_DEFAULT_REGION=!AWS_REGION!"

set "ACCOUNT_ID="
aws sts get-caller-identity --query Account --output text > "%TEMP%\cu_acct.txt"
if !ERRORLEVEL! neq 0 (
  echo Could not determine AWS account id. Check your AWS credentials.
  exit /b 1
)
set /p ACCOUNT_ID=<"%TEMP%\cu_acct.txt"
echo AWS account: !ACCOUNT_ID!

rem ---- Cluster ----
set "CLUSTER_STATUS="
aws ecs describe-clusters --clusters !CLUSTER_NAME! --region !AWS_REGION! --query "clusters[0].status" --output text > "%TEMP%\cu_cluster.txt" 2>nul
set /p CLUSTER_STATUS=<"%TEMP%\cu_cluster.txt"
if not "!CLUSTER_STATUS!"=="ACTIVE" (
  echo Creating ECS cluster !CLUSTER_NAME! ...
  aws ecs create-cluster --cluster-name !CLUSTER_NAME! --region !AWS_REGION! >nul
  if !ERRORLEVEL! neq 0 (
    echo Cluster creation failed
    exit /b 1
  )
)

rem ---- Log group ----
aws logs create-log-group --log-group-name !LOG_GROUP! --region !AWS_REGION! >nul 2>&1

rem ---- Load balancer ----
set "TARGET_GROUP_ARN="
set "ALB_DNS="
set "WANT_LB=0"
if /i "!NEED_LB!"=="y" set "WANT_LB=1"
if "!WANT_LB!"=="1" (
  call :create_lb
  if errorlevel 1 exit /b 1
)

rem ---- Render manifests ----
set "WORK_DIR=%TEMP%\cu-ecs-%RANDOM%"
mkdir "!WORK_DIR!"
set "IMAGE_URI=!IMAGE_URI!"
for %%F in (task-definition.json service-definition.json) do (
  powershell -NoProfile -Command "$c=Get-Content -Raw '!ECS_DIR!\%%F'; foreach($k in 'IMAGE_URI','AWS_REGION','ACCOUNT_ID','SUBNET_1','SUBNET_2','SECURITY_GROUP','CLUSTER_NAME','DB_SECRET_ARN','TARGET_GROUP_ARN'){ $v=[Environment]::GetEnvironmentVariable($k); if($null -eq $v){$v=''}; $c=$c.Replace('{{'+$k+'}}',$v) }; [IO.File]::WriteAllText('!WORK_DIR!\%%F',$c,(New-Object Text.UTF8Encoding($false)))"
  if !ERRORLEVEL! neq 0 (
    echo Failed to render %%F
    exit /b 1
  )
)

if "!WANT_LB!"=="0" (
  echo No load balancer requested - removing loadBalancers from service definition
  powershell -NoProfile -Command "$j=Get-Content -Raw '!WORK_DIR!\service-definition.json' | ConvertFrom-Json; $j.PSObject.Properties.Remove('loadBalancers'); $j.PSObject.Properties.Remove('healthCheckGracePeriodSeconds'); [IO.File]::WriteAllText('!WORK_DIR!\service-definition.json',($j | ConvertTo-Json -Depth 10),(New-Object Text.UTF8Encoding($false)))"
  if !ERRORLEVEL! neq 0 (
    echo Failed to edit service definition
    exit /b 1
  )
)

rem ---- Task definition ----
echo Registering task definition ...
set "TASK_DEF_ARN="
aws ecs register-task-definition --cli-input-json file://!WORK_DIR!\task-definition.json --region !AWS_REGION! --query "taskDefinition.taskDefinitionArn" --output text > "%TEMP%\cu_td.txt"
if !ERRORLEVEL! neq 0 (
  echo Task definition registration failed
  exit /b 1
)
set /p TASK_DEF_ARN=<"%TEMP%\cu_td.txt"
echo Task definition: !TASK_DEF_ARN!

rem ---- Service ----
set "EXISTING_SERVICE="
aws ecs describe-services --cluster !CLUSTER_NAME! --services !SERVICE_NAME! --region !AWS_REGION! --query "services[?status=='ACTIVE'].serviceName | [0]" --output text > "%TEMP%\cu_svc.txt" 2>nul
set /p EXISTING_SERVICE=<"%TEMP%\cu_svc.txt"

if "!EXISTING_SERVICE!"=="" goto createsvc
if "!EXISTING_SERVICE!"=="None" goto createsvc

echo Updating service !SERVICE_NAME! ...
aws ecs update-service --cluster !CLUSTER_NAME! --service !SERVICE_NAME! --task-definition !TASK_DEF_ARN! --force-new-deployment --region !AWS_REGION! >nul
if !ERRORLEVEL! neq 0 (
  echo Service update failed
  exit /b 1
)
goto waitsvc

:createsvc
echo Creating service !SERVICE_NAME! ...
powershell -NoProfile -Command "$j=Get-Content -Raw '!WORK_DIR!\service-definition.json' | ConvertFrom-Json; $j.taskDefinition=$env:TASK_DEF_ARN; [IO.File]::WriteAllText('!WORK_DIR!\service-definition.json',($j | ConvertTo-Json -Depth 10),(New-Object Text.UTF8Encoding($false)))"
aws ecs create-service --cli-input-json file://!WORK_DIR!\service-definition.json --region !AWS_REGION! >nul
if !ERRORLEVEL! neq 0 (
  echo Service creation failed
  exit /b 1
)

:waitsvc
echo Waiting for service to become stable. This can take several minutes ...
aws ecs wait services-stable --cluster !CLUSTER_NAME! --services !SERVICE_NAME! --region !AWS_REGION!
if !ERRORLEVEL! neq 0 (
  echo Service did not become stable. Check ECS events and CloudWatch logs.
  exit /b 1
)

echo.
echo === Deployment verification ===
aws ecs describe-services --cluster !CLUSTER_NAME! --services !SERVICE_NAME! --region !AWS_REGION! --query "services[0].{Status:status,Desired:desiredCount,Running:runningCount,Pending:pendingCount,TaskDefinition:taskDefinition}" --output table
echo CloudWatch log group: !LOG_GROUP!
if not "!ALB_DNS!"=="" echo Application URL: http://!ALB_DNS!
echo Deployment completed successfully.
rmdir /s /q "!WORK_DIR!" >nul 2>&1
endlocal
exit /b 0

:badsubnets
echo Two comma-separated subnet IDs are required
exit /b 1

:missing
echo All values are required
exit /b 1

:create_lb
set "ALB_NAME=!PROJECT_NAME!-alb"
set "TG_NAME=!PROJECT_NAME!-tg"
set "ALB_ARN="
aws elbv2 describe-load-balancers --names !ALB_NAME! --region !AWS_REGION! --query "LoadBalancers[0].LoadBalancerArn" --output text > "%TEMP%\cu_alb.txt" 2>nul
set /p ALB_ARN=<"%TEMP%\cu_alb.txt"
if "!ALB_ARN!"=="" goto mk_alb
if "!ALB_ARN!"=="None" goto mk_alb
goto have_alb
:mk_alb
echo Creating Application Load Balancer !ALB_NAME! ...
set "ALB_ARN="
aws elbv2 create-load-balancer --name !ALB_NAME! --subnets !SUBNET_1! !SUBNET_2! --security-groups !SECURITY_GROUP! --scheme internet-facing --type application --region !AWS_REGION! --query "LoadBalancers[0].LoadBalancerArn" --output text > "%TEMP%\cu_alb.txt"
if !ERRORLEVEL! neq 0 (
  echo Load balancer creation failed
  exit /b 1
)
set /p ALB_ARN=<"%TEMP%\cu_alb.txt"
:have_alb
set "ALB_DNS="
aws elbv2 describe-load-balancers --load-balancer-arns !ALB_ARN! --region !AWS_REGION! --query "LoadBalancers[0].DNSName" --output text > "%TEMP%\cu_dns.txt"
set /p ALB_DNS=<"%TEMP%\cu_dns.txt"

set "TARGET_GROUP_ARN="
aws elbv2 describe-target-groups --names !TG_NAME! --region !AWS_REGION! --query "TargetGroups[0].TargetGroupArn" --output text > "%TEMP%\cu_tg.txt" 2>nul
set /p TARGET_GROUP_ARN=<"%TEMP%\cu_tg.txt"
if "!TARGET_GROUP_ARN!"=="" goto mk_tg
if "!TARGET_GROUP_ARN!"=="None" goto mk_tg
goto have_tg
:mk_tg
echo Creating target group !TG_NAME! with target-type ip ...
set "TARGET_GROUP_ARN="
aws elbv2 create-target-group --name !TG_NAME! --protocol HTTP --port !CONTAINER_PORT! --vpc-id !VPC_ID! --target-type ip --health-check-protocol HTTP --health-check-path !HEALTH_PATH! --health-check-port traffic-port --matcher HttpCode=200 --region !AWS_REGION! --query "TargetGroups[0].TargetGroupArn" --output text > "%TEMP%\cu_tg.txt"
if !ERRORLEVEL! neq 0 (
  echo Target group creation failed
  exit /b 1
)
set /p TARGET_GROUP_ARN=<"%TEMP%\cu_tg.txt"
:have_tg

set "LISTENER_ARN="
aws elbv2 describe-listeners --load-balancer-arn !ALB_ARN! --region !AWS_REGION! --query "Listeners[0].ListenerArn" --output text > "%TEMP%\cu_lsn.txt" 2>nul
set /p LISTENER_ARN=<"%TEMP%\cu_lsn.txt"
if "!LISTENER_ARN!"=="" goto mk_lsn
if "!LISTENER_ARN!"=="None" goto mk_lsn
exit /b 0
:mk_lsn
echo Creating HTTP 80 listener ...
aws elbv2 create-listener --load-balancer-arn !ALB_ARN! --protocol HTTP --port 80 --default-actions Type=forward,TargetGroupArn=!TARGET_GROUP_ARN! --region !AWS_REGION! >nul
if !ERRORLEVEL! neq 0 (
  echo Listener creation failed
  exit /b 1
)
exit /b 0
