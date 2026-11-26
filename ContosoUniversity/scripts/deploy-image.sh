#!/bin/bash
set -e
set -o pipefail

# Deploy ContosoUniversity to AWS EKS.
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="${SCRIPT_DIR}/../kubernetes"

for cmd in aws kubectl; do
  command -v "$cmd" >/dev/null 2>&1 || { echo "ERROR: $cmd is required but not installed."; exit 1; }
done

read -r -p "AWS region [us-east-1]: " AWS_REGION
AWS_REGION=${AWS_REGION:-us-east-1}
read -r -p "EKS cluster name: " CLUSTER_NAME
[ -n "$CLUSTER_NAME" ] || { echo "ERROR: cluster name is required."; exit 1; }
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$IMAGE_URI" ] || { echo "ERROR: image URI is required."; exit 1; }

echo "Application configuration (press Enter to skip optional values)"
read -r -p "Enter value for DB_HOST (or press Enter to skip): " DB_HOST
read -r -p "Enter value for DB_NAME (or press Enter to skip) [ContosoUniversity]: " DB_NAME
DB_NAME=${DB_NAME:-ContosoUniversity}
read -r -p "Enter value for DB_USER (or press Enter to skip): " DB_USER
read -r -s -p "Enter value for DB_PASSWORD (or press Enter to skip): " DB_PASSWORD
echo

# Work on a temporary copy so the template manifests stay untouched
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

sed -i 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{DB_HOST}}|'"$DB_HOST"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{DB_NAME}}|'"$DB_NAME"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{DB_USER}}|'"$DB_USER"'|g' "$WORK_DIR/deployment.yaml"

echo "Configuring kubectl for cluster ${CLUSTER_NAME}..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach the cluster."; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

kubectl create secret generic "${APP_NAME}-db" \
  --from-literal=DB_PASSWORD="$DB_PASSWORD" \
  -n "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/${APP_NAME} -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. Roll back with:"
  echo "  kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}"
  echo "Inspect with:"
  echo "  kubectl describe pods -n ${NAMESPACE}"
  echo "  kubectl logs -l app=${APP_NAME} -n ${NAMESPACE} --tail=100"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

echo "Waiting for the load balancer address (up to ~3 minutes)..."
ALB_HOST=""
for i in $(seq 1 18); do
  ALB_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
  [ -n "$ALB_HOST" ] && break
  sleep 10
done

if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://${ALB_HOST}"
else
  echo "Ingress address not yet assigned. Check later with:"
  echo "  kubectl get ingress -n ${NAMESPACE}"
  echo "(Requires the AWS Load Balancer Controller to be installed in the cluster.)"
fi
