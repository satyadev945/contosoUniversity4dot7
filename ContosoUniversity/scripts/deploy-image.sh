#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from the module directory or repository root.
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"

for tool in aws kubectl; do
  command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: $tool is not installed"; exit 1; }
done

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$AWS_REGION" ] && [ -n "$CLUSTER_NAME" ] && [ -n "$IMAGE_URI" ] || { echo "Region, cluster name and image URI are required"; exit 1; }

read -r -p "Ingress host name (or press Enter for contosouniversity.example.com): " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}

read -r -s -p "Enter value for SCHOOL_DB_CONNECTION_STRING (SQL Server connection string, or press Enter to skip): " SCHOOL_DB_CONNECTION_STRING
echo

# Work on a temp copy so the templates keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

ESC_IMAGE=$(printf '%s' "$IMAGE_URI" | sed 's/[&|]/\\&/g')
ESC_HOST=$(printf '%s' "$INGRESS_HOST" | sed 's/[&|]/\\&/g')
sed -i 's|{{IMAGE_URI}}|'"$ESC_IMAGE"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{INGRESS_HOST}}|'"$ESC_HOST"'|g' "$WORK_DIR/ingress.yaml"

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach the cluster"; exit 1; }

echo "Applying namespace..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

if [ -n "$SCHOOL_DB_CONNECTION_STRING" ]; then
  echo "Creating/updating secret..."
  kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
    --from-literal=SCHOOL_DB_CONNECTION_STRING="$SCHOOL_DB_CONNECTION_STRING" \
    --dry-run=client -o yaml | kubectl apply -f -
else
  echo "No connection string supplied; skipping secret creation."
fi

echo "Applying deployment, service and ingress..."
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/"$APP_NAME" -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs -l app=$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

ALB=$(kubectl get ingress "${APP_NAME}-ingress" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB" ]; then
  echo "Application URL: http://$ALB  (Host header: $INGRESS_HOST)"
else
  echo "ALB address not yet assigned. Check later: kubectl get ingress -n $NAMESPACE"
fi
