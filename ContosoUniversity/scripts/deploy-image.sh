#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Can be run from any directory.
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR/../kubernetes"

for tool in aws kubectl base64; do
  command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: $tool is required but not installed."; exit 1; }
done

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
if [ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ]; then
  echo "ERROR: region, cluster name and image URI are required."; exit 1
fi
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): " CONNECTION_STRING
echo
if [ -z "$CONNECTION_STRING" ]; then
  echo "ERROR: the application cannot start without a database connection string."; exit 1
fi

# Render manifests into a temp dir so the templates stay untouched
RENDER_DIR=$(mktemp -d)
trap 'rm -rf "$RENDER_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$RENDER_DIR"/

CONNECTION_STRING_B64=$(printf '%s' "$CONNECTION_STRING" | base64 | tr -d '\n')
ESCAPED_IMAGE=$(printf '%s' "$IMAGE_URI" | sed -e 's/[|&\\]/\\&/g')

sed -i 's|{{IMAGE_URI}}|'"$ESCAPED_IMAGE"'|g' "$RENDER_DIR/deployment.yaml"
sed -i 's|{{CONNECTION_STRING_B64}}|'"$CONNECTION_STRING_B64"'|g' "$RENDER_DIR/deployment.yaml"
sed -i 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$RENDER_DIR"/*.yaml

echo "Configuring kubectl for cluster $CLUSTER_NAME..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach the cluster."; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$RENDER_DIR/namespace.yaml"
kubectl apply -f "$RENDER_DIR/deployment.yaml"
kubectl apply -f "$RENDER_DIR/service.yaml"
kubectl apply -f "$RENDER_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "ERROR: rollout failed. Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  echo "Rollback with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

echo "Waiting for the load balancer address (up to ~3 minutes)..."
ALB=""
for _ in $(seq 1 18); do
  ALB=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
  [ -n "$ALB" ] && break
  sleep 10
done
if [ -n "$ALB" ]; then
  echo "Application URL: http://$ALB"
else
  echo "Ingress address not yet assigned. Check: kubectl get ingress -n $NAMESPACE (requires AWS Load Balancer Controller)"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
