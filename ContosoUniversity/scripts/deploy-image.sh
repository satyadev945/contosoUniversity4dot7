#!/bin/bash
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"

read -r -p "Azure resource group: " RESOURCE_GROUP
[ -z "$RESOURCE_GROUP" ] && { echo "Resource group is required"; exit 1; }
read -r -p "AKS cluster name: " CLUSTER_NAME
[ -z "$CLUSTER_NAME" ] && { echo "Cluster name is required"; exit 1; }
read -r -p "Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): " IMAGE_URI
[ -z "$IMAGE_URI" ] && { echo "Image URI is required"; exit 1; }
read -r -p "Kubernetes namespace [contosouniversity]: " NAMESPACE
NAMESPACE=${NAMESPACE:-contosouniversity}
read -r -p "Ingress host [contosouniversity.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}
read -r -p "Enter value for DB_HOST (SQL Server host, or press Enter to skip): " DB_HOST
read -r -p "Enter value for DB_NAME (or press Enter for ContosoUniversity): " DB_NAME
read -r -p "Enter value for DB_USER (or press Enter for sa): " DB_USER
read -r -s -p "Enter value for DB_PASSWORD (or press Enter to skip): " DB_PASSWORD; echo
read -r -s -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " AI_CONN; echo
DB_HOST=${DB_HOST:-localhost}
DB_NAME=${DB_NAME:-ContosoUniversity}
DB_USER=${DB_USER:-sa}

esc() { printf '%s' "$1" | sed -e 's/[&|\\]/\\&/g'; }

WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{NAMESPACE}}|$(esc "$NAMESPACE")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{INGRESS_HOST}}|$(esc "$INGRESS_HOST")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_HOST}}|$(esc "$DB_HOST")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_NAME}}|$(esc "$DB_NAME")|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_USER}}|$(esc "$DB_USER")|g" "$WORK_DIR"/*.yaml

echo ">> Configuring kubectl for $CLUSTER_NAME ..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

echo ">> Applying manifests ..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
  --from-literal=DB_PASSWORD="$DB_PASSWORD" \
  --from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_CONN" \
  --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo ">> Waiting for rollout ..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. Roll back with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

ADDRESS=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
echo "Application URL: http://${INGRESS_HOST}  (ingress address: ${ADDRESS:-pending})"
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
