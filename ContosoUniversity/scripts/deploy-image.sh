#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from the ContosoUniversity module directory or the repo root.
set -e
set -o pipefail

echo "=== ContosoUniversity - Deploy to AWS EKS ==="

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

for cmd in aws kubectl; do
  command -v "$cmd" >/dev/null 2>&1 || { echo "ERROR: $cmd is required but not installed."; exit 1; }
done

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ] && { echo "ERROR: region, cluster name and image URI are required."; exit 1; }

read -r -p "ASPNETCORE_ENVIRONMENT [Production]: " ASPNETCORE_ENVIRONMENT
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, required): " CONNECTION_STRING
echo
[ -z "$CONNECTION_STRING" ] && { echo "ERROR: connection string is required (the app fails to start without it)."; exit 1; }
read -r -p "Enter value for SKIP_DB_INIT (true/false, or press Enter for false): " SKIP_DB_INIT
SKIP_DB_INIT=${SKIP_DB_INIT:-false}

# Escape characters special to sed replacement (\ & |)
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }

# Work on a temporary copy so templates keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp -r "$K8S_SRC"/. "$WORK_DIR"/

sed -i 's|{{IMAGE_URI}}|'"$(esc "$IMAGE_URI")"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{NAMESPACE}}|'"$(esc "$NAMESPACE")"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{ASPNETCORE_ENVIRONMENT}}|'"$(esc "$ASPNETCORE_ENVIRONMENT")"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{SKIP_DB_INIT}}|'"$(esc "$SKIP_DB_INIT")"'|g' "$WORK_DIR/deployment.yaml"
ESC_CONN=$(printf '%s' "$CONNECTION_STRING" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g')
sed -i 's|{{CONNECTION_STRING}}|'"$(esc "$ESC_CONN")"'|g' "$WORK_DIR/deployment.yaml"

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach cluster."; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "ERROR: rollout failed. Recent pod state:"
  kubectl get pods -n $NAMESPACE
  echo "To inspect:  kubectl logs -l app=$APP_NAME -n $NAMESPACE --tail=100"
  echo "To roll back: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

INGRESS_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$INGRESS_HOST" ]; then
  echo "Application URL: http://$INGRESS_HOST"
else
  echo "Ingress address not yet assigned. Check later with: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
