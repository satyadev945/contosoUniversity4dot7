#!/bin/bash
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODULE_DIR="$(dirname "$SCRIPT_DIR")"
K8S_SRC="$MODULE_DIR/kubernetes"

APP_NAME="contoso-university"
NAMESPACE="contoso-university"

echo "=== ContosoUniversity - Deploy to Azure AKS ==="

command -v az >/dev/null 2>&1 || { echo "ERROR: Azure CLI (az) is not installed"; exit 1; }
command -v kubectl >/dev/null 2>&1 || { echo "ERROR: kubectl is not installed"; exit 1; }

read -r -p "Enter Azure resource group name: " RESOURCE_GROUP
[ -n "$RESOURCE_GROUP" ] || { echo "ERROR: Resource group is required"; exit 1; }
read -r -p "Enter AKS cluster name: " CLUSTER_NAME
[ -n "$CLUSTER_NAME" ] || { echo "ERROR: AKS cluster name is required"; exit 1; }
read -r -p "Enter Docker image URI (e.g. myacr.azurecr.io/contosouniversity:latest): " IMAGE_URI
[ -n "$IMAGE_URI" ] || { echo "ERROR: Image URI is required"; exit 1; }
read -r -p "Enter ingress host name [${APP_NAME}.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-${APP_NAME}.example.com}

echo "Application settings (press Enter to skip)"
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): " SCHOOL_CONNECTION_STRING
echo
read -r -s -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " AI_CONNECTION_STRING
echo

# Render manifests into a temp directory so source files stay untouched
RENDER_DIR=$(mktemp -d)
trap 'rm -rf "$RENDER_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$RENDER_DIR"/

sed_escape() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }
IMAGE_URI_ESC=$(sed_escape "$IMAGE_URI")
INGRESS_HOST_ESC=$(sed_escape "$INGRESS_HOST")
NAMESPACE_ESC=$(sed_escape "$NAMESPACE")

for f in "$RENDER_DIR"/*.yaml; do
  sed -i.bak "s|{{IMAGE_URI}}|${IMAGE_URI_ESC}|g; s|{{INGRESS_HOST}}|${INGRESS_HOST_ESC}|g; s|{{NAMESPACE}}|${NAMESPACE_ESC}|g" "$f"
  rm -f "$f.bak"
done

echo "Configuring kubectl for AKS cluster $CLUSTER_NAME..."
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --overwrite-existing

echo "Verifying cluster connectivity..."
kubectl cluster-info || exit 1

echo "Applying namespace..."
kubectl apply -f "$RENDER_DIR/namespace.yaml"

echo "Creating/updating application secret..."
kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
  --from-literal=ConnectionStrings__SchoolContext="$SCHOOL_CONNECTION_STRING" \
  --from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "Applying deployment, service and ingress..."
kubectl apply -f "$RENDER_DIR/deployment.yaml"
kubectl apply -f "$RENDER_DIR/service.yaml"
kubectl apply -f "$RENDER_DIR/ingress.yaml"

echo "Waiting for rollout to complete..."
if ! kubectl rollout status deployment/${APP_NAME} -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: Rollout failed. Inspect with: kubectl describe pods -n $NAMESPACE"
  echo "Rollback with: kubectl rollout undo deployment/${APP_NAME} -n $NAMESPACE"
  exit 1
fi

echo "Deployed resources:"
kubectl get pods,svc,ingress -n "$NAMESPACE"

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
echo
echo "Application URL: http://${INGRESS_HOST}"
if [ -n "$INGRESS_IP" ]; then
  echo "Ingress IP: $INGRESS_IP (point DNS for ${INGRESS_HOST} to this address)"
else
  echo "Ingress IP not assigned yet. Check: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/${APP_NAME} -n $NAMESPACE"
