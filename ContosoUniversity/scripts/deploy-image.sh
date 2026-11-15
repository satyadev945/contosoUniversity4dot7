#!/bin/bash
# Deploy ContosoUniversity to AWS ECS Fargate.
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ECS_DIR="$(dirname "$SCRIPT_DIR")/ecs"
PROJECT_NAME="contosouniversity"
SERVICE_NAME="${PROJECT_NAME}-service"
TASK_FAMILY="${PROJECT_NAME}-task"
LOG_GROUP="/ecs/${PROJECT_NAME}"
CONTAINER_PORT=8080
HEALTH_PATH="/health"

trap 'echo ""; echo "Deployment failed. Hints: check IAM permissions, that subnets/security group belong to the VPC, that the image URI exists, and CloudWatch log group ${LOG_GROUP}."' ERR

for tool in aws jq sed; do
  command -v "$tool" >/dev/null 2>&1 || { echo "Required tool '$tool' not found in PATH"; exit 1; }
done

read -r -p "Enter AWS region (e.g., us-east-1): " AWS_REGION
read -r -p "Enter ECS cluster name (e.g., my-ecs-cluster): " CLUSTER_NAME
read -r -p "Enter VPC ID (e.g., vpc-0abc123def456): " VPC_ID
read -r -p "Enter Subnet IDs comma-separated, at least two (e.g., subnet-0abc123,subnet-0def456): " SUBNET_IDS
read -r -p "Enter Security Group ID (e.g., sg-0abc123def): " SECURITY_GROUP
read -r -p "Enter Docker image URI (e.g., 123456789.dkr.ecr.us-east-1.amazonaws.com/contosouniversity:latest): " IMAGE_URI
read -r -p "Enter Secrets Manager/SSM ARN holding the SQL Server connection string (ConnectionStrings__SchoolContext): " DB_SECRET_ARN
read -r -p "Do you need a load balancer for this service? (y/n): " NEED_LB

SUBNET_1=$(echo "$SUBNET_IDS" | cut -d',' -f1 | tr -d ' ')
SUBNET_2=$(echo "$SUBNET_IDS" | cut -d',' -f2 | tr -d ' ')
if [ -z "$SUBNET_1" ] || [ -z "$SUBNET_2" ]; then
  echo "Two comma-separated subnet IDs are required"
  exit 1
fi
if [ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$SECURITY_GROUP" ] || [ -z "$IMAGE_URI" ] || [ -z "$DB_SECRET_ARN" ]; then
  echo "All values are required"
  exit 1
fi
export AWS_DEFAULT_REGION="$AWS_REGION"

ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
echo "AWS account: $ACCOUNT_ID"

# ---- Cluster ----
CLUSTER_STATUS=$(aws ecs describe-clusters --clusters "$CLUSTER_NAME" --region "$AWS_REGION" --query 'clusters[0].status' --output text 2>/dev/null || true)
if [ "$CLUSTER_STATUS" != "ACTIVE" ]; then
  echo "Creating ECS cluster $CLUSTER_NAME ..."
  aws ecs create-cluster --cluster-name "$CLUSTER_NAME" --region "$AWS_REGION" >/dev/null
else
  echo "ECS cluster $CLUSTER_NAME already exists"
fi

# ---- CloudWatch log group ----
aws logs create-log-group --log-group-name "$LOG_GROUP" --region "$AWS_REGION" 2>/dev/null || echo "Log group $LOG_GROUP already exists"

# ---- Load balancer ----
TARGET_GROUP_ARN=""
ALB_DNS=""
if [ "$NEED_LB" = "y" ] || [ "$NEED_LB" = "Y" ]; then
  ALB_NAME="${PROJECT_NAME}-alb"
  TG_NAME="${PROJECT_NAME}-tg"

  ALB_ARN=$(aws elbv2 describe-load-balancers --names "$ALB_NAME" --region "$AWS_REGION" --query 'LoadBalancers[0].LoadBalancerArn' --output text 2>/dev/null || true)
  if [ -z "$ALB_ARN" ] || [ "$ALB_ARN" = "None" ]; then
    echo "Creating Application Load Balancer $ALB_NAME ..."
    ALB_ARN=$(aws elbv2 create-load-balancer --name "$ALB_NAME" --subnets "$SUBNET_1" "$SUBNET_2" \
      --security-groups "$SECURITY_GROUP" --scheme internet-facing --type application \
      --region "$AWS_REGION" --query 'LoadBalancers[0].LoadBalancerArn' --output text)
  else
    echo "Load balancer $ALB_NAME already exists"
  fi
  ALB_DNS=$(aws elbv2 describe-load-balancers --load-balancer-arns "$ALB_ARN" --region "$AWS_REGION" --query 'LoadBalancers[0].DNSName' --output text)

  TARGET_GROUP_ARN=$(aws elbv2 describe-target-groups --names "$TG_NAME" --region "$AWS_REGION" --query 'TargetGroups[0].TargetGroupArn' --output text 2>/dev/null || true)
  if [ -z "$TARGET_GROUP_ARN" ] || [ "$TARGET_GROUP_ARN" = "None" ]; then
    echo "Creating target group $TG_NAME (target-type ip) ..."
    TARGET_GROUP_ARN=$(aws elbv2 create-target-group --name "$TG_NAME" --protocol HTTP --port "$CONTAINER_PORT" \
      --vpc-id "$VPC_ID" --target-type ip --health-check-protocol HTTP --health-check-path "$HEALTH_PATH" \
      --health-check-port traffic-port --matcher HttpCode=200 \
      --region "$AWS_REGION" --query 'TargetGroups[0].TargetGroupArn' --output text)
  else
    echo "Target group $TG_NAME already exists"
  fi

  LISTENER_ARN=$(aws elbv2 describe-listeners --load-balancer-arn "$ALB_ARN" --region "$AWS_REGION" --query 'Listeners[0].ListenerArn' --output text 2>/dev/null || true)
  if [ -z "$LISTENER_ARN" ] || [ "$LISTENER_ARN" = "None" ]; then
    echo "Creating HTTP:80 listener ..."
    aws elbv2 create-listener --load-balancer-arn "$ALB_ARN" --protocol HTTP --port 80 \
      --default-actions "Type=forward,TargetGroupArn=${TARGET_GROUP_ARN}" --region "$AWS_REGION" >/dev/null
  fi
fi

# ---- Render manifests in a temp dir ----
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$ECS_DIR/task-definition.json" "$WORK_DIR/task-definition.json"
cp "$ECS_DIR/service-definition.json" "$WORK_DIR/service-definition.json"

for f in "$WORK_DIR/task-definition.json" "$WORK_DIR/service-definition.json"; do
  sed -i.bak \
    -e "s|{{IMAGE_URI}}|${IMAGE_URI}|g" \
    -e "s|{{AWS_REGION}}|${AWS_REGION}|g" \
    -e "s|{{ACCOUNT_ID}}|${ACCOUNT_ID}|g" \
    -e "s|{{SUBNET_1}}|${SUBNET_1}|g" \
    -e "s|{{SUBNET_2}}|${SUBNET_2}|g" \
    -e "s|{{SECURITY_GROUP}}|${SECURITY_GROUP}|g" \
    -e "s|{{CLUSTER_NAME}}|${CLUSTER_NAME}|g" \
    -e "s|{{DB_SECRET_ARN}}|${DB_SECRET_ARN}|g" \
    -e "s|{{TARGET_GROUP_ARN}}|${TARGET_GROUP_ARN}|g" \
    "$f"
  rm -f "$f.bak"
done

if [ -z "$TARGET_GROUP_ARN" ]; then
  echo "No load balancer requested: removing loadBalancers from service definition"
  jq 'del(.loadBalancers, .healthCheckGracePeriodSeconds)' "$WORK_DIR/service-definition.json" > "$WORK_DIR/service.tmp"
  mv "$WORK_DIR/service.tmp" "$WORK_DIR/service-definition.json"
fi

# ---- Task definition ----
echo "Registering task definition $TASK_FAMILY ..."
TASK_DEF_ARN=$(aws ecs register-task-definition --cli-input-json "file://$WORK_DIR/task-definition.json" \
  --region "$AWS_REGION" --query 'taskDefinition.taskDefinitionArn' --output text)
echo "Task definition: $TASK_DEF_ARN"

# ---- Service ----
EXISTING_SERVICE=$(aws ecs describe-services --cluster "$CLUSTER_NAME" --services "$SERVICE_NAME" --region "$AWS_REGION" \
  --query "services[?status=='ACTIVE'].serviceName | [0]" --output text 2>/dev/null || true)

if [ -z "$EXISTING_SERVICE" ] || [ "$EXISTING_SERVICE" = "None" ]; then
  echo "Creating service $SERVICE_NAME ..."
  jq --arg td "$TASK_DEF_ARN" '.taskDefinition = $td' "$WORK_DIR/service-definition.json" > "$WORK_DIR/service.tmp"
  mv "$WORK_DIR/service.tmp" "$WORK_DIR/service-definition.json"
  aws ecs create-service --cli-input-json "file://$WORK_DIR/service-definition.json" --region "$AWS_REGION" >/dev/null
else
  echo "Updating service $SERVICE_NAME ..."
  aws ecs update-service --cluster "$CLUSTER_NAME" --service "$SERVICE_NAME" --task-definition "$TASK_DEF_ARN" \
    --force-new-deployment --region "$AWS_REGION" >/dev/null
fi

echo "Waiting for service to become stable (this can take several minutes) ..."
aws ecs wait services-stable --cluster "$CLUSTER_NAME" --services "$SERVICE_NAME" --region "$AWS_REGION"

echo ""
echo "=== Deployment verification ==="
aws ecs describe-services --cluster "$CLUSTER_NAME" --services "$SERVICE_NAME" --region "$AWS_REGION" \
  --query 'services[0].{Status:status,Desired:desiredCount,Running:runningCount,Pending:pendingCount,TaskDefinition:taskDefinition}' --output table

echo "CloudWatch log group: $LOG_GROUP"
if [ -n "$ALB_DNS" ]; then
  echo "Application URL: http://${ALB_DNS}"
fi
echo "Deployment completed successfully."
