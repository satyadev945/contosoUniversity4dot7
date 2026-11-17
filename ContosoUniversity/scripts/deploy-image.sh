#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from the ContosoUniversity project directory.
set -e
set -o pipefail

APP_NAME="contosouniversity"
NAMESPACE="contosouniversity"
K8S_DIR="$(cd "$(dirname "$0")/../kubernetes" && pwd)"

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
[ -n "$AWS_REGION" ] && [ -n "$CLUSTER_NAME" ] && [ -n "$IMAGE_URI" ] || { echo "Region, cluster and image URI are required"; exit 1; }

echo "Configuring kubectl..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "Cannot reach cluster"; exit 1; }

echo "Applying namespace..."
kubectl apply -f "$K8S_DIR/namespace.yaml"

read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string, Enter to keep existing secret): " DB_CONN
echo
if [ -n "$DB_CONN" ]; then
  kubectl create secret generic contosouniversity-db -n "$NAMESPACE" \
    --from-literal=ConnectionStrings__SchoolContext="$DB_CONN" \
    --dry-run=client -o yaml | kubectl apply -f -
elif ! kubectl get secret contosouniversity-db -n "$NAMESPACE" >/dev/null 2>&1; then
  echo "Secret contosouniversity-db does not exist and no connection string was given."; exit 1
fi

echo "Applying deployment, service and ingress..."
sed 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g; s|{{NAMESPACE}}|'"$NAMESPACE"'|g' "$K8S_DIR/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/service.yaml"
kubectl apply -f "$K8S_DIR/ingress.yaml"

echo "Waiting for rollout..."
if ! kubectl rollout status deployment/"$APP_NAME" -n "$NAMESPACE" --timeout=300s; then
  echo "Rollout failed. Roll back with: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"
HOST=$(kubectl get ingress "$APP_NAME-ingress" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$HOST" ]; then echo "Application URL: http://$HOST"; else echo "Ingress address not ready yet; re-run: kubectl get ingress -n $NAMESPACE"; fi
