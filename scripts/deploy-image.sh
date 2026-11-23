#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from the repository root.
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

echo "=== Deploy $APP_NAME to AWS EKS ==="
read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$AWS_REGION" ] && [ -n "$CLUSTER_NAME" ] && [ -n "$IMAGE_URI" ] || { echo "Region, cluster name and image URI are required"; exit 1; }

echo "Application configuration (press Enter to skip)"
read -r -p "Enter value for DB_HOST (or press Enter to skip): " DB_HOST
read -r -p "Enter value for DB_NAME (or press Enter to skip) [ContosoUniversity]: " DB_NAME
read -r -p "Enter value for DB_USER (or press Enter to skip): " DB_USER
read -r -s -p "Enter value for DB_PASSWORD (or press Enter to skip): " DB_PASSWORD; echo
read -r -s -p "Enter value for CONNECTION_STRING (full SQL connection string, or press Enter to skip): " CONNECTION_STRING; echo
DB_NAME=${DB_NAME:-ContosoUniversity}

# Escape characters special to sed replacement text (| & \)
esc() { printf '%s' "$1" | sed -e 's/[\\|&]/\\&/g'; }

WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp kubernetes/*.yaml "$WORK_DIR"/

sed -i "s|{{IMAGE_URI}}|$(esc "$IMAGE_URI")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{NAMESPACE}}|$NAMESPACE|g" "$WORK_DIR"/*.yaml
sed -i "s|{{DB_HOST}}|$(esc "$DB_HOST")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_NAME}}|$(esc "$DB_NAME")|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DB_USER}}|$(esc "$DB_USER")|g" "$WORK_DIR/deployment.yaml"

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

kubectl create secret generic contosouniversity-secrets -n "$NAMESPACE" \
  --from-literal=DB_PASSWORD="$DB_PASSWORD" \
  --from-literal=CONNECTION_STRING="$CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs -l app=$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

ALB_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://$ALB_HOST"
else
  echo "Ingress address not yet assigned. Check: kubectl get ingress -n $NAMESPACE"
fi
