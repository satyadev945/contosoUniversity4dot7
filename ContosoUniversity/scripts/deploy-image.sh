#!/bin/bash
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_SRC="$SCRIPT_DIR/../kubernetes"
APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
[ -z "$AWS_REGION" ] && { echo "AWS region is required"; exit 1; }
read -r -p "EKS cluster name: " CLUSTER_NAME
[ -z "$CLUSTER_NAME" ] && { echo "Cluster name is required"; exit 1; }
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -z "$IMAGE_URI" ] && { echo "Image URI is required"; exit 1; }
read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, or press Enter to skip): " CONNECTION_STRING
echo

# Escape characters special to sed replacement (\, &, |)
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }
# Escape for YAML double-quoted string
yamlesc() { printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g'; }

WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_SRC"/*.yaml "$WORK_DIR"/

if [ -z "$CONNECTION_STRING" ]; then
  CONNECTION_STRING="Server=localhost,1433;Database=ContosoUniversity;User Id=sa;Password=changeme;TrustServerCertificate=True"
  echo "WARNING: using default placeholder connection string; the app will not reach a database."
fi

sed -i 's|{{IMAGE_URI}}|'"$(esc "$IMAGE_URI")"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{NAMESPACE}}|'"$(esc "$NAMESPACE")"'|g' "$WORK_DIR"/*.yaml
sed -i 's|{{CONNECTION_STRING}}|'"$(esc "$(yamlesc "$CONNECTION_STRING")")"'|g' "$WORK_DIR/deployment.yaml"

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "Rollout failed. Roll back with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

ALB=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB" ]; then
  echo "Application URL: http://$ALB"
else
  echo "ALB address not yet assigned. Check: kubectl get ingress -n $NAMESPACE"
fi
