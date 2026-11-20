#!/bin/bash
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"

if [ ! -d "$K8S_SRC" ]; then echo "ERROR: kubernetes manifests not found at $K8S_SRC"; exit 1; fi

read -r -p "Enter Azure resource group: " RESOURCE_GROUP
read -r -p "Enter AKS cluster name: " CLUSTER_NAME
read -r -p "Enter Docker image URI (registry/repo:tag): " IMAGE_URI
if [ -z "$RESOURCE_GROUP" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ]; then
  echo "ERROR: resource group, cluster name and image URI are required"; exit 1
fi

read -r -p "Enter value for DB_HOST (SQL Server host) (or press Enter to skip): " DB_HOST
read -r -p "Enter value for DB_PORT [1433]: " DB_PORT
read -r -p "Enter value for DB_NAME [ContosoUniversity]: " DB_NAME
read -r -p "Enter value for DB_USER (or press Enter to skip): " DB_USER
read -r -s -p "Enter value for DB_PASSWORD (or press Enter to skip): " DB_PASSWORD; echo
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (full connection string, or press Enter to skip): " CONNECTION_STRING; echo
read -r -p "Run DbInitializer on startup, DB_INITIALIZE (true/false) [false]: " DB_INITIALIZE
read -r -p "Enter ingress host name [contosouniversity.example.com]: " INGRESS_HOST

DB_PORT=${DB_PORT:-1433}
DB_NAME=${DB_NAME:-ContosoUniversity}
DB_INITIALIZE=${DB_INITIALIZE:-false}
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}

# Work on a temporary copy so templates keep their placeholders
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT
cp -r "$K8S_SRC"/. "$WORK_DIR"/

for f in "$WORK_DIR"/*.yaml; do
  sed -i 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$f"
  sed -i 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$f"
  sed -i 's|{{DB_HOST}}|'"$DB_HOST"'|g' "$f"
  sed -i 's|{{DB_PORT}}|'"$DB_PORT"'|g' "$f"
  sed -i 's|{{DB_NAME}}|'"$DB_NAME"'|g' "$f"
  sed -i 's|{{DB_USER}}|'"$DB_USER"'|g' "$f"
  sed -i 's|{{DB_INITIALIZE}}|'"$DB_INITIALIZE"'|g' "$f"
  sed -i 's|{{INGRESS_HOST}}|'"$INGRESS_HOST"'|g' "$f"
done

echo "Configuring kubectl for AKS cluster $CLUSTER_NAME..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing

echo "Verifying cluster connectivity..."
kubectl cluster-info || exit 1

echo "Applying namespace..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

echo "Creating/updating database secret..."
kubectl create secret generic contosouniversity-db -n "$NAMESPACE" \
  --from-literal=DB_PASSWORD="$DB_PASSWORD" \
  --from-literal=CONNECTION_STRING="$CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "Applying deployment, service and ingress..."
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. Roll back with:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs -l app=$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
echo "Application host: http://$INGRESS_HOST"
if [ -n "$INGRESS_IP" ]; then echo "Ingress IP: $INGRESS_IP"; else echo "Ingress IP not yet assigned; re-run: kubectl get ingress -n $NAMESPACE"; fi
