#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from the repository root.
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"

read -r -p "Enter AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "Enter EKS cluster name: " CLUSTER_NAME
read -r -p "Enter Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$AWS_REGION" ] && [ -n "$CLUSTER_NAME" ] && [ -n "$IMAGE_URI" ] || { echo "Region, cluster name and image URI are required"; exit 1; }

read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): " SCHOOL_DB_CONNECTION
echo
[ -n "$SCHOOL_DB_CONNECTION" ] || { echo "A database connection string is required"; exit 1; }
read -r -p "Enter value for Database__AutoInitialize (true/false, press Enter for true): " AUTO_INIT
AUTO_INIT=${AUTO_INIT:-true}

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

# Render manifests into a temp dir so the templates stay untouched
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp kubernetes/*.yaml "$WORK_DIR/"
sed -i "s|{{IMAGE_URI}}|$IMAGE_URI|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{DATABASE_AUTOINITIALIZE}}|$AUTO_INIT|g" "$WORK_DIR/deployment.yaml"
sed -i "s|{{NAMESPACE}}|$NAMESPACE|g" "$WORK_DIR"/*.yaml

echo "Applying manifests..."
kubectl apply -f "$WORK_DIR/namespace.yaml"
kubectl create secret generic contosouniversity-secrets -n "$NAMESPACE" \
  --from-literal=SchoolContext="$SCHOOL_DB_CONNECTION" --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/$APP_NAME -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. To roll back: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect: kubectl describe pods -n $NAMESPACE ; kubectl logs -l app=$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"
INGRESS_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$INGRESS_HOST" ]; then
  echo "Application URL: http://$INGRESS_HOST"
else
  echo "Ingress address not yet assigned (requires AWS Load Balancer Controller). Re-check: kubectl get ingress -n $NAMESPACE"
fi
echo "Rollback if needed: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
