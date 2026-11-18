#!/bin/bash
set -e
set -o pipefail

# Deploy ContosoUniversity to AWS EKS
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

command -v aws >/dev/null 2>&1 || { echo "aws cli is required"; exit 1; }
command -v kubectl >/dev/null 2>&1 || { echo "kubectl is required"; exit 1; }

read -r -p "Enter AWS region (e.g. us-east-1): " AWS_REGION
[ -n "$AWS_REGION" ] || { echo "AWS region is required"; exit 1; }
read -r -p "Enter EKS cluster name: " CLUSTER_NAME
[ -n "$CLUSTER_NAME" ] || { echo "EKS cluster name is required"; exit 1; }
read -r -p "Enter Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$IMAGE_URI" ] || { echo "Image URI is required"; exit 1; }
read -r -p "Enter ingress host name [contosouniversity.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}
read -r -s -p "Enter value for SCHOOL_DB_CONNECTION (SQL Server connection string): " SCHOOL_DB_CONNECTION
echo
read -r -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " APPLICATIONINSIGHTS_CONNECTION_STRING

# Escape characters special to sed replacement (backslash, ampersand, pipe)
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }

# Work on a temporary copy so the template manifests keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{SCHOOL_DB_CONNECTION}}|$(esc "$SCHOOL_DB_CONNECTION")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{APPLICATIONINSIGHTS_CONNECTION_STRING}}|$(esc "$APPLICATIONINSIGHTS_CONNECTION_STRING")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{INGRESS_HOST}}|$(esc "$INGRESS_HOST")|g" "$WORK_DIR/ingress.yaml"
sed -i "s|{{NAMESPACE}}|$NAMESPACE|g" "$WORK_DIR"/*.yaml

echo "Configuring kubectl for cluster $CLUSTER_NAME..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "Cannot reach the cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "Rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

ALB_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://$ALB_HOST (Host header: $INGRESS_HOST)"
else
  echo "Load balancer address not yet assigned. Check later with: kubectl get ingress -n $NAMESPACE"
fi
