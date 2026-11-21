#!/bin/bash
# Deploy ContosoUniversity to GKE. Run from the ContosoUniversity module directory
# or the repository root; manifests are located relative to this script.
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="${SCRIPT_DIR}/../kubernetes"

read -r -p "GCP project ID: " GCP_PROJECT
read -r -p "GKE cluster zone (or region): " GCP_ZONE
read -r -p "GKE cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/path:tag): " IMAGE_URI
for v in GCP_PROJECT GCP_ZONE CLUSTER_NAME IMAGE_URI; do
  if [ -z "${!v}" ]; then echo "ERROR: $v is required"; exit 1; fi
done
read -r -s -p "Enter value for SCHOOL_DB_CONNECTION (SQL Server connection string): " SCHOOL_DB_CONNECTION
echo
if [ -z "$SCHOOL_DB_CONNECTION" ]; then echo "ERROR: SCHOOL_DB_CONNECTION is required (the app fails to start without it)"; exit 1; fi

WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$WORK_DIR"/

# Escape characters special to sed replacement text
ESC_IMAGE=$(printf '%s' "$IMAGE_URI" | sed -e 's/[&|\\]/\\&/g')
sed -i.bak "s|{{IMAGE_URI}}|${ESC_IMAGE}|g" "$WORK_DIR/deployment.yaml"
sed -i.bak "s|{{NAMESPACE}}|${NAMESPACE}|g" "$WORK_DIR"/*.yaml
rm -f "$WORK_DIR"/*.bak

echo "Configuring kubectl..."
gcloud container clusters get-credentials "$CLUSTER_NAME" --zone "$GCP_ZONE" --project "$GCP_PROJECT"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach cluster"; exit 1; }

echo "Applying namespace..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

echo "Creating/updating secret..."
kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
  --from-literal=SCHOOL_DB_CONNECTION="$SCHOOL_DB_CONNECTION" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "Applying deployment, service, ingress..."
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/${APP_NAME} -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}"
  echo "Inspect: kubectl describe pods -n ${NAMESPACE}; kubectl logs deployment/${APP_NAME} -n ${NAMESPACE}"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
if [ -n "$INGRESS_IP" ]; then
  echo "Application URL: http://${INGRESS_IP}/"
else
  echo "Ingress IP not yet assigned (can take several minutes). Check: kubectl get ingress -n ${NAMESPACE}"
fi
echo "Rollback if needed: kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}"
