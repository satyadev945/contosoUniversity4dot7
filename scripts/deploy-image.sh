#!/bin/bash
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/kubernetes"

echo "=== Deploy ${APP_NAME} to Azure AKS ==="
read -r -p "Enter Azure resource group: " RESOURCE_GROUP
read -r -p "Enter AKS cluster name: " CLUSTER_NAME
read -r -p "Enter Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): " IMAGE_URI
[ -z "$RESOURCE_GROUP" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ] && { echo "Resource group, cluster name and image URI are required"; exit 1; }

read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, or press Enter to skip): " SCHOOL_CONNECTION_STRING
echo
read -r -s -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " APPINSIGHTS_CS
echo

# Work on a temporary copy so repository manifests keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$SRC_DIR"/*.yaml "$WORK_DIR"/

# Escape characters special to sed replacement (\ & |)
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }
IMAGE_ESC=$(esc "$IMAGE_URI")
DB_ESC=$(esc "$SCHOOL_CONNECTION_STRING")
AI_ESC=$(esc "$APPINSIGHTS_CS")

sed -i "s|{{IMAGE_URI}}|${IMAGE_ESC}|g" "$WORK_DIR"/*.yaml
sed -i "s|{{NAMESPACE}}|${NAMESPACE}|g" "$WORK_DIR"/*.yaml
sed -i "s|{{SCHOOL_CONNECTION_STRING}}|${DB_ESC}|g" "$WORK_DIR"/*.yaml
sed -i "s|{{APPLICATIONINSIGHTS_CONNECTION_STRING}}|${AI_ESC}|g" "$WORK_DIR"/*.yaml

echo "Configuring kubectl..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing
kubectl cluster-info || { echo "Cannot reach the cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/${APP_NAME} -n ${NAMESPACE} --timeout=300s; then
  echo "Rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}"
  echo "Inspect with: kubectl describe pods -n ${NAMESPACE}; kubectl logs deployment/${APP_NAME} -n ${NAMESPACE}"
  exit 1
fi

kubectl get pods,svc,ingress -n ${NAMESPACE}

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n ${NAMESPACE} -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
if [ -n "$INGRESS_IP" ]; then
  echo "Application URL: http://${INGRESS_IP}/  (host header: contosouniversity.example.com)"
else
  echo "Ingress address not assigned yet. Check: kubectl get ingress -n ${NAMESPACE}"
fi
echo "Rollback if needed: kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}"
