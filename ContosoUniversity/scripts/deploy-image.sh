#!/bin/bash
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR/../kubernetes"

for cmd in aws kubectl; do
  command -v "$cmd" >/dev/null 2>&1 || { echo "ERROR: $cmd is required but not installed"; exit 1; }
done

read -p "AWS region (e.g. us-east-1): " AWS_REGION
[ -n "$AWS_REGION" ] || { echo "AWS region is required"; exit 1; }
read -p "EKS cluster name: " CLUSTER_NAME
[ -n "$CLUSTER_NAME" ] || { echo "EKS cluster name is required"; exit 1; }
read -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$IMAGE_URI" ] || { echo "Image URI is required"; exit 1; }
read -p "Ingress host [contosouniversity.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}
read -s -p "Enter value for SCHOOL_DB_CONNECTION - SQL Server connection string (or press Enter to skip): " SCHOOL_DB_CONNECTION
echo

# Work on a temp copy so the template manifests keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$WORK_DIR"/

sed -i.bak 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$WORK_DIR/deployment.yaml"
sed -i.bak 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$WORK_DIR"/*.yaml
sed -i.bak 's|{{INGRESS_HOST}}|'"$INGRESS_HOST"'|g' "$WORK_DIR/ingress.yaml"
rm -f "$WORK_DIR"/*.bak

echo "Configuring kubectl for cluster $CLUSTER_NAME..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

# Connection string is stored in a Secret (not in the manifest)
kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
  --from-literal=SCHOOL_DB_CONNECTION="$SCHOOL_DB_CONNECTION" \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "ERROR: rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

ALB_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://$ALB_HOST (Host header: $INGRESS_HOST)"
else
  echo "Ingress address not yet assigned. Check: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
