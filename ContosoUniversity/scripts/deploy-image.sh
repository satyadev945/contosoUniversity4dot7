#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from anywhere:
#   bash ContosoUniversity/scripts/deploy-image.sh
set -e
set -o pipefail

APP_NAME="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR/../kubernetes"

for tool in aws kubectl; do
  command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: $tool is not installed or not in PATH."; exit 1; }
done

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
read -r -p "Kubernetes namespace [$APP_NAME]: " NAMESPACE
NAMESPACE=${NAMESPACE:-$APP_NAME}

if [ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ]; then
  echo "ERROR: AWS region, cluster name and image URI are required."
  exit 1
fi

read -r -s -p "Enter value for ConnectionStrings__SchoolContext - SQL Server connection string (or press Enter to skip): " DB_CONNECTION
echo

# Work on a temporary copy so the template manifests keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$WORK_DIR"/

sed -i.bak 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$WORK_DIR"/*.yaml
sed -i.bak 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$WORK_DIR"/*.yaml
rm -f "$WORK_DIR"/*.bak

echo "Configuring kubectl for cluster $CLUSTER_NAME..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach the cluster."; exit 1; }

echo "Applying namespace..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

if [ -n "$DB_CONNECTION" ]; then
  echo "Creating/updating secret ${APP_NAME}-secrets..."
  kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
    --from-literal=ConnectionStrings__SchoolContext="$DB_CONNECTION" \
    --dry-run=client -o yaml | kubectl apply -f -
fi

echo "Applying deployment, service and ingress..."
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status "deployment/$APP_NAME" -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

echo "Waiting for load balancer address (up to ~3 minutes)..."
ALB_HOST=""
for i in $(seq 1 18); do
  ALB_HOST=$(kubectl get ingress "${APP_NAME}-ingress" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
  [ -n "$ALB_HOST" ] && break
  sleep 10
done

if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://$ALB_HOST"
else
  echo "Ingress address not yet available. Check: kubectl get ingress -n $NAMESPACE"
  echo "(Requires the AWS Load Balancer Controller to be installed in the cluster.)"
fi
