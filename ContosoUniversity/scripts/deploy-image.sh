#!/bin/bash
# Deploy ContosoUniversity to AWS EKS.
#   ./ContosoUniversity/scripts/deploy-image.sh
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="${SCRIPT_DIR}/../kubernetes"

for tool in aws kubectl; do
  command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: $tool is required but not installed." >&2; exit 1; }
done

read -r -p "AWS region [us-east-1]: " AWS_REGION
AWS_REGION=${AWS_REGION:-us-east-1}
read -r -p "EKS cluster name: " CLUSTER_NAME
[ -n "$CLUSTER_NAME" ] || { echo "ERROR: EKS cluster name is required." >&2; exit 1; }
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$IMAGE_URI" ] || { echo "ERROR: image URI is required." >&2; exit 1; }
read -r -p "Ingress host name [contosouniversity.example.com]: " INGRESS_HOST
INGRESS_HOST=${INGRESS_HOST:-contosouniversity.example.com}
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, or press Enter to skip): " CONNECTION_STRING
echo

# Work on a temporary copy so the template manifests keep their placeholders
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT
cp -r "$K8S_SRC"/. "$WORK_DIR"/

sed -i.bak "s|{{IMAGE_URI}}|${IMAGE_URI}|g" "$WORK_DIR/deployment.yaml"
sed -i.bak "s|{{NAMESPACE}}|${NAMESPACE}|g" "$WORK_DIR"/*.yaml
sed -i.bak "s|{{INGRESS_HOST}}|${INGRESS_HOST}|g" "$WORK_DIR/ingress.yaml"
rm -f "$WORK_DIR"/*.bak

echo "Configuring kubectl for cluster ${CLUSTER_NAME}..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: cannot reach the cluster." >&2; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

if [ -n "$CONNECTION_STRING" ]; then
  kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
    --from-literal="ConnectionStrings__SchoolContext=${CONNECTION_STRING}" \
    --dry-run=client -o yaml | kubectl apply -f -
fi

kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status "deployment/${APP_NAME}" -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. Inspect with:" >&2
  echo "  kubectl describe pods -n ${NAMESPACE}" >&2
  echo "  kubectl logs deployment/${APP_NAME} -n ${NAMESPACE}" >&2
  echo "Roll back with: kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}" >&2
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

echo ""
echo "Waiting for the ALB address (can take a few minutes)..."
ALB_HOST=""
for _ in $(seq 1 30); do
  ALB_HOST=$(kubectl get ingress "${APP_NAME}-ingress" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
  [ -n "$ALB_HOST" ] && break
  sleep 10
done

if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://${ALB_HOST}  (Host header: ${INGRESS_HOST})"
else
  echo "ALB address not ready yet. Check: kubectl get ingress -n ${NAMESPACE}"
fi
echo "Rollback if needed: kubectl rollout undo deployment/${APP_NAME} -n ${NAMESPACE}"
