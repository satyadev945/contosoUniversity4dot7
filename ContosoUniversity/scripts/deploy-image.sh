#!/bin/bash
set -e
set -o pipefail

# Run from the ContosoUniversity module directory or repository root
if [ -d "kubernetes" ]; then K8S_DIR="kubernetes"; elif [ -d "ContosoUniversity/kubernetes" ]; then K8S_DIR="ContosoUniversity/kubernetes"; else echo "kubernetes directory not found"; exit 1; fi

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ] && { echo "Region, cluster and image URI are required"; exit 1; }

read -r -p "Enter value for DB_HOST (SQL Server host): " DB_HOST
read -r -p "Enter value for DB_PORT [1433]: " DB_PORT; DB_PORT=${DB_PORT:-1433}
read -r -p "Enter value for DB_NAME [ContosoUniversity]: " DB_NAME; DB_NAME=${DB_NAME:-ContosoUniversity}
read -r -p "Enter value for DB_USER [sa]: " DB_USER; DB_USER=${DB_USER:-sa}
read -r -s -p "Enter value for DB_PASSWORD: " DB_PASSWORD; echo
[ -z "$DB_HOST" ] && { echo "DB_HOST is required"; exit 1; }

# Work on a temp copy so templates keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$WORK_DIR"/

esc() { printf '%s' "$1" | sed -e 's/[&|\\]/\\&/g'; }
sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_HOST}}|$(esc "$DB_HOST")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_PORT}}|$(esc "$DB_PORT")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_NAME}}|$(esc "$DB_NAME")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_USER}}|$(esc "$DB_USER")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_PASSWORD}}|$(esc "$DB_PASSWORD")|g" "$WORK_DIR/deployment.yaml"

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "Rollout failed. Roll back with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

ALB=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB" ]; then echo "Application URL: http://$ALB"; else echo "Ingress address not yet assigned; check: kubectl get ingress -n $NAMESPACE"; fi
echo "Rollback: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
