#!/bin/bash
# Deploy ContosoUniversity to Azure AKS.
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

for tool in az kubectl; do
  command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: $tool is not installed or not in PATH"; exit 1; }
done

read -r -p "Azure resource group: " RESOURCE_GROUP
[ -n "$RESOURCE_GROUP" ] || { echo "ERROR: resource group is required"; exit 1; }
read -r -p "AKS cluster name: " CLUSTER_NAME
[ -n "$CLUSTER_NAME" ] || { echo "ERROR: cluster name is required"; exit 1; }
read -r -p "Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): " IMAGE_URI
[ -n "$IMAGE_URI" ] || { echo "ERROR: image URI is required"; exit 1; }
read -r -p "Ingress host name [contosouniversity.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}

echo "--- Application configuration (SQL Server) ---"
read -r -p "Enter value for DB_HOST (or press Enter to skip): " DB_HOST
read -r -p "Enter value for DB_NAME (or press Enter for ContosoUniversity): " DB_NAME
DB_NAME=${DB_NAME:-ContosoUniversity}
read -r -p "Enter value for DB_USER (or press Enter for sa): " DB_USER
DB_USER=${DB_USER:-sa}
read -r -s -p "Enter value for DB_PASSWORD (or press Enter to skip): " DB_PASSWORD
echo
read -r -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " APPINSIGHTS

# Escape a value for safe use inside a double-quoted YAML string and a sed replacement (| delimiter)
esc() {
  printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/[&|\\]/\\&/g'
}

# Work on a temp copy so the template manifests keep their {{PLACEHOLDERS}}
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$WORK_DIR"/

sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{INGRESS_HOST}}|$(esc "$INGRESS_HOST")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_HOST}}|$(esc "$DB_HOST")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_NAME}}|$(esc "$DB_NAME")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_USER}}|$(esc "$DB_USER")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_PASSWORD}}|$(esc "$DB_PASSWORD")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{APPLICATIONINSIGHTS_CONNECTION_STRING}}|$(esc "$APPINSIGHTS")|g" "$WORK_DIR"/*.yaml

echo ">> Configuring kubectl for $CLUSTER_NAME ..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing || { echo "ERROR: failed to get AKS credentials"; exit 1; }

echo ">> Verifying cluster connectivity ..."
kubectl cluster-info || { echo "ERROR: cannot reach the cluster"; exit 1; }

echo ">> Applying manifests ..."
kubectl apply -f "$WORK_DIR/namespace.yaml" || exit 1
kubectl apply -f "$WORK_DIR/deployment.yaml" || exit 1
kubectl apply -f "$WORK_DIR/service.yaml" || exit 1
kubectl apply -f "$WORK_DIR/ingress.yaml" || exit 1

echo ">> Waiting for rollout ..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "ERROR: rollout failed."
  echo "Inspect: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  echo "Rollback: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

echo ">> Resources:"
kubectl get pods,svc,ingress -n $NAMESPACE

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
echo
echo "Application host : http://$INGRESS_HOST"
if [ -n "$INGRESS_IP" ]; then
  echo "Ingress IP       : $INGRESS_IP (point DNS for $INGRESS_HOST here, or use: curl -H 'Host: $INGRESS_HOST' http://$INGRESS_IP/health)"
else
  echo "Ingress IP is not assigned yet. Check: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
