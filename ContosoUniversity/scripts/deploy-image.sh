#!/bin/bash
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
# Run from the ContosoUniversity module directory or repository root
if [ -d "kubernetes" ]; then K8S_DIR="kubernetes"; else K8S_DIR="ContosoUniversity/kubernetes"; fi

read -p "AWS region (e.g. us-east-1): " AWS_REGION
read -p "EKS cluster name: " CLUSTER_NAME
read -p "Docker image URI (full path with tag): " IMAGE_URI
read -p "Enter value for ConnectionStrings__SchoolContext (or press Enter to skip): " CONN_STR

[ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ] && { echo "Region, cluster name and image URI are required"; exit 1; }

# Work on a temp copy so templates keep their placeholders
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp "$K8S_DIR"/*.yaml "$WORK_DIR"/

sed -i 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$WORK_DIR/deployment.yaml"
sed -i 's|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$WORK_DIR"/*.yaml
ESC_CONN=$(printf '%s' "$CONN_STR" | sed 's/[&|\\]/\\&/g')
sed -i 's|{{CONNECTIONSTRINGS__SCHOOLCONTEXT}}|'"$ESC_CONN"'|g' "$WORK_DIR/deployment.yaml"

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"

echo "Verifying cluster connectivity..."
kubectl cluster-info || exit 1

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s; then
  echo "Rollout failed. To roll back: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n $NAMESPACE

ALB_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$ALB_HOST" ]; then
  echo "Application URL: http://$ALB_HOST"
else
  echo "Ingress address not yet assigned. Check: kubectl get ingress -n $NAMESPACE"
fi
