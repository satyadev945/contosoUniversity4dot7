#!/bin/bash
set -e
set -o pipefail

# Deploys ContosoUniversity to Azure AKS.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

echo "=== ContosoUniversity - Deploy to Azure AKS ==="

read -r -p "Azure resource group: " RESOURCE_GROUP
[ -z "$RESOURCE_GROUP" ] && { echo "ERROR: resource group is required"; exit 1; }
read -r -p "AKS cluster name: " CLUSTER_NAME
[ -z "$CLUSTER_NAME" ] && { echo "ERROR: cluster name is required"; exit 1; }
read -r -p "Docker image URI (registry/image:tag): " IMAGE_URI
[ -z "$IMAGE_URI" ] && { echo "ERROR: image URI is required"; exit 1; }

echo "--- Application configuration (SQL Server) - press Enter to skip/use default ---"
read -r -p "Enter value for DB_HOST (or press Enter to skip): " DB_HOST
read -r -p "Enter value for DB_PORT (default 1433): " DB_PORT
read -r -p "Enter value for DB_NAME (default ContosoUniversity): " DB_NAME
read -r -p "Enter value for DB_USER (default sa): " DB_USER
read -r -s -p "Enter value for DB_PASSWORD (or press Enter to skip): " DB_PASSWORD
echo
read -r -p "Ingress host name (default contosouniversity.example.com): " INGRESS_HOST
DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-1433}
DB_NAME=${DB_NAME:-ContosoUniversity}
DB_USER=${DB_USER:-sa}
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}

# Work on a temporary copy so the template manifests stay untouched
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

echo "Updating manifests ..."
for f in "$WORK_DIR"/*.yaml; do
  sed -i 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$f"
  sed -i 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$f"
  sed -i 's|{{DB_HOST}}|'"$DB_HOST"'|g' "$f"
  sed -i 's|{{DB_PORT}}|'"$DB_PORT"'|g' "$f"
  sed -i 's|{{DB_NAME}}|'"$DB_NAME"'|g' "$f"
  sed -i 's|{{DB_USER}}|'"$DB_USER"'|g' "$f"
  sed -i 's|{{INGRESS_HOST}}|'"$INGRESS_HOST"'|g' "$f"
done

echo "Configuring kubectl ..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing
kubectl cluster-info || { echo "ERROR: cannot reach cluster"; exit 1; }

echo "Applying manifests ..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

# DB password is stored in a Kubernetes secret (not in the manifest)
kubectl create secret generic contosouniversity-db \
  --namespace "$NAMESPACE" \
  --from-literal=DB_PASSWORD="$DB_PASSWORD" \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout ..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. Roll back with:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
echo "=== Deployment complete ==="
if [ -n "$INGRESS_IP" ]; then
  echo "Application URL: http://$INGRESS_IP/  (host header: $INGRESS_HOST)"
else
  echo "Ingress address pending. Run: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
