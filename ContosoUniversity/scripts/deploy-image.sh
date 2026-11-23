#!/bin/bash
set -e
set -o pipefail

APP_NAME="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"

# Escape a value for use in a sed replacement that uses | as delimiter
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }

echo "=== ContosoUniversity - Deploy to Azure AKS ==="

read -r -p "Azure resource group: " RESOURCE_GROUP
read -r -p "AKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
read -r -p "Kubernetes namespace [contosouniversity]: " NAMESPACE
NAMESPACE=${NAMESPACE:-contosouniversity}

if [ -z "$RESOURCE_GROUP" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ]; then
  echo "ERROR: resource group, cluster name and image URI are required"; exit 1
fi

read -r -s -p "Enter value for CONNECTION_STRING - ConnectionStrings__SchoolContext (or press Enter to skip): " CONNECTION_STRING
echo
read -r -p "Enter value for APPINSIGHTS_CONNECTION_STRING (or press Enter to skip): " APPINSIGHTS_CONNECTION_STRING

# Work on a copy so the template manifests keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

sed -i 's|{{IMAGE_URI}}|'"$(esc "$IMAGE_URI")"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{NAMESPACE}}|'"$(esc "$NAMESPACE")"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{CONNECTION_STRING}}|'"$(esc "$CONNECTION_STRING")"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{APPINSIGHTS_CONNECTION_STRING}}|'"$(esc "$APPINSIGHTS_CONNECTION_STRING")"'|g' "$WORK_DIR"/*.yaml

echo "Configuring kubectl..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing
kubectl cluster-info || { echo "ERROR: cannot reach the cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/"$APP_NAME" -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. Inspect with: kubectl describe pods -n $NAMESPACE"
  echo "Roll back with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

INGRESS_IP=$(kubectl get ingress "${APP_NAME}-ingress" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
if [ -n "$INGRESS_IP" ]; then
  echo "Application address: http://$INGRESS_IP (send Host header contosouniversity.example.com or edit the ingress host)"
else
  echo "Ingress address not assigned yet. Check: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
