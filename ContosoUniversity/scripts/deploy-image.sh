#!/bin/bash
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
read -r -p "Kubernetes namespace [$APP_NAME]: " NAMESPACE
NAMESPACE=${NAMESPACE:-$APP_NAME}
read -r -p "ASPNETCORE_ENVIRONMENT [Production]: " ASPNETCORE_ENVIRONMENT
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): " CONNECTION_STRING
echo

[ -n "$AWS_REGION" ] && [ -n "$CLUSTER_NAME" ] && [ -n "$IMAGE_URI" ] || { echo "Region, cluster name and image URI are required"; exit 1; }
[ -n "$CONNECTION_STRING" ] || { echo "Connection string is required (the app will not start without it)"; exit 1; }

CONN_B64=$(printf '%s' "$CONNECTION_STRING" | base64 | tr -d '\n')

# Render manifests into a temp dir so the templates stay untouched
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/
esc() { printf '%s' "$1" | sed -e 's/[&|\\]/\\&/g'; }
sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{NAMESPACE}}|$(esc "$NAMESPACE")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{ASPNETCORE_ENVIRONMENT}}|$(esc "$ASPNETCORE_ENVIRONMENT")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{CONNECTION_STRING_B64}}|$CONN_B64|g" "$WORK_DIR"/*.yaml

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. Roll back with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

ALB=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB" ]; then
  echo "Application URL: http://$ALB"
else
  echo "ALB address not yet assigned. Check: kubectl get ingress -n $NAMESPACE"
fi
