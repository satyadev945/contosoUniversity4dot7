#!/bin/bash
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"

echo "=== ContosoUniversity: Deploy to Azure AKS ==="

read -r -p "Azure resource group: " RESOURCE_GROUP
[ -z "$RESOURCE_GROUP" ] && { echo "Resource group is required"; exit 1; }
read -r -p "AKS cluster name: " CLUSTER_NAME
[ -z "$CLUSTER_NAME" ] && { echo "Cluster name is required"; exit 1; }
read -r -p "Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): " IMAGE_URI
[ -z "$IMAGE_URI" ] && { echo "Image URI is required"; exit 1; }
read -r -p "Kubernetes namespace [$APP_NAME]: " NAMESPACE
NAMESPACE=${NAMESPACE:-$APP_NAME}
read -r -p "Ingress host [$APP_NAME.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-$APP_NAME.example.com}

read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, required): " CONNECTION_STRING
echo
[ -z "$CONNECTION_STRING" ] && { echo "Connection string is required"; exit 1; }
read -r -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " APPINSIGHTS_CONNECTION_STRING

# Escape characters special to sed replacement text
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g' -e 's/"/\\"/g'; }

# Work on a temporary copy so the template manifests stay untouched
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$SRC_DIR"/*.yaml "$WORK_DIR"/

IMAGE_ESC=$(esc "$IMAGE_URI")
CONN_ESC=$(esc "$CONNECTION_STRING")
AI_ESC=$(esc "$APPINSIGHTS_CONNECTION_STRING")
for f in "$WORK_DIR"/*.yaml; do
  sed -i "s|{{IMAGE_URI}}|$IMAGE_ESC|g" "$f"
  sed -i "s|{{NAMESPACE}}|$NAMESPACE|g" "$f"
  sed -i "s|{{INGRESS_HOST}}|$INGRESS_HOST|g" "$f"
  sed -i "s|{{CONNECTION_STRING}}|$CONN_ESC|g" "$f"
  sed -i "s|{{APPINSIGHTS_CONNECTION_STRING}}|$AI_ESC|g" "$f"
done

echo "Configuring kubectl..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing
kubectl cluster-info || { echo "Cannot connect to cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. Inspect with: kubectl describe pods -n $NAMESPACE"
  echo "Rollback with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

ADDRESS=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
echo "Application URL: http://${INGRESS_HOST}  (ingress address: ${ADDRESS:-pending})"
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
