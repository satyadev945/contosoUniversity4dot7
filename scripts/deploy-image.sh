#!/bin/bash
set -e
set -o pipefail

# Deploy ContosoUniversity to AWS EKS. Run from the repository root.
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
[ -z "$AWS_REGION" ] && { echo "AWS region is required"; exit 1; }
read -r -p "EKS cluster name: " CLUSTER_NAME
[ -z "$CLUSTER_NAME" ] && { echo "EKS cluster name is required"; exit 1; }
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -z "$IMAGE_URI" ] && { echo "Image URI is required"; exit 1; }

echo "Application configuration (press Enter to skip optional values)"
read -r -s -p "Enter value for DB_CONNECTION_STRING (SQL Server connection string, required): " DB_CONNECTION_STRING
echo
[ -z "$DB_CONNECTION_STRING" ] && { echo "DB_CONNECTION_STRING is required by the application"; exit 1; }
read -r -s -p "Enter value for APPLICATIONINSIGHTS_CONNECTION_STRING (or press Enter to skip): " AI_CONNECTION_STRING
echo

# Work on a temp copy so the template manifests keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp kubernetes/*.yaml "$WORK_DIR/"
sed -i.bak 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$WORK_DIR/deployment.yaml"
sed -i.bak 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$WORK_DIR"/*.yaml

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "Cannot reach the cluster"; exit 1; }

echo "Applying namespace..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

echo "Creating/Updating application secret..."
kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
  --from-literal=DB_CONNECTION_STRING="$DB_CONNECTION_STRING" \
  --from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "Applying deployment, service and ingress..."
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. Roll back with:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

echo "Waiting for ALB address (may take a few minutes)..."
for i in $(seq 1 30); do
  HOST=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
  [ -n "$HOST" ] && break
  sleep 10
done
if [ -n "$HOST" ]; then
  echo "Application URL: http://$HOST"
else
  echo "Ingress address not assigned yet. Check: kubectl get ingress -n $NAMESPACE"
fi
