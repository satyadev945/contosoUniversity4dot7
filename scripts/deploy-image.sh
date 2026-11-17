#!/bin/bash
set -e
set -o pipefail
# Deploy ContosoUniversity to GCP GKE. Run from the repository root.

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

echo "=== ContosoUniversity GKE deployment ==="
read -r -p "Enter GCP project ID: " GCP_PROJECT
read -r -p "Enter GCP zone (e.g. us-central1-a): " GCP_ZONE
read -r -p "Enter GKE cluster name: " CLUSTER_NAME
read -r -p "Enter Docker image URI (full path with tag): " IMAGE_URI

for v in GCP_PROJECT GCP_ZONE CLUSTER_NAME IMAGE_URI; do
  if [ -z "${!v}" ]; then echo "ERROR: $v is required"; exit 1; fi
done

echo "Enter value for SCHOOL_CONNECTION_STRING (SQL Server connection string, or press Enter to skip)"
read -r -p "> " SCHOOL_CONNECTION_STRING
SCHOOL_CONNECTION_STRING=${SCHOOL_CONNECTION_STRING:-Server=localhost;Database=ContosoUniversity;TrustServerCertificate=True}

# Escape characters special to sed replacement text (| delimiter, & and backslash)
esc() { printf '%s' "$1" | sed -e 's/[\\|&]/\\&/g'; }

cp kubernetes/*.yaml "$WORK_DIR"/
for f in "$WORK_DIR"/*.yaml; do
  sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$f"
  sed -i "s|{{NAMESPACE}}|$NAMESPACE|g" "$f"
  sed -i "s|{{SCHOOL_CONNECTION_STRING}}|$(esc "$SCHOOL_CONNECTION_STRING")|g" "$f"
done

echo "Configuring kubectl..."
gcloud container clusters get-credentials "$CLUSTER_NAME" --zone "$GCP_ZONE" --project "$GCP_PROJECT"
kubectl cluster-info || { echo "ERROR: cannot reach cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "ERROR: rollout failed. Inspect: kubectl describe pods -n $NAMESPACE"
  echo "Rollback: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

INGRESS_IP=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)
if [ -n "$INGRESS_IP" ]; then
  echo "Application URL: http://$INGRESS_IP (host: contosouniversity.example.com)"
else
  echo "Ingress IP is still provisioning. Check: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
