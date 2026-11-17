#!/bin/bash
# Deploy ContosoUniversity to AWS EKS. Run from the repository root.
set -e
set -o pipefail

APP_NAME="contoso-university"
NAMESPACE="contoso-university"

read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
read -r -p "EKS cluster name: " CLUSTER_NAME
read -r -p "Docker image URI (registry/repo:tag): " IMAGE_URI
if [ -z "$AWS_REGION" ] || [ -z "$CLUSTER_NAME" ] || [ -z "$IMAGE_URI" ]; then
  echo "ERROR: region, cluster name and image URI are required"; exit 1
fi

read -r -s -p "Enter value for ConnectionStrings__SchoolContext (SQL Server connection string): " CONNECTION_STRING
echo
if [ -z "$CONNECTION_STRING" ]; then
  echo "ERROR: the application requires a SQL Server connection string"; exit 1
fi

echo "Configuring kubectl for cluster $CLUSTER_NAME ..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
kubectl cluster-info || { echo "ERROR: cannot reach cluster"; exit 1; }

# Render manifests into a temp dir so the templates stay untouched
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT
cp kubernetes/*.yaml "$WORK_DIR/"
sed -i 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' "$WORK_DIR/deployment.yaml"

echo "Applying namespace ..."
kubectl apply -f "$WORK_DIR/namespace.yaml"

echo "Creating/updating secret ..."
kubectl create secret generic "${APP_NAME}-secrets" -n "$NAMESPACE" \
  --from-literal=ConnectionStrings__SchoolContext="$CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "Applying deployment, service, ingress ..."
kubectl apply -f "$WORK_DIR/deployment.yaml"
kubectl apply -f "$WORK_DIR/service.yaml"
kubectl apply -f "$WORK_DIR/ingress.yaml"

echo "Waiting for rollout ..."
if ! kubectl rollout status deployment/"$APP_NAME" -n "$NAMESPACE" --timeout=300s; then
  echo "ERROR: rollout failed. To roll back run:"
  echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
  echo "Inspect with: kubectl describe pods -n $NAMESPACE ; kubectl logs deployment/$APP_NAME -n $NAMESPACE"
  exit 1
fi

kubectl get pods,svc,ingress -n "$NAMESPACE"

INGRESS_HOST=$(kubectl get ingress ${APP_NAME}-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)
if [ -n "$INGRESS_HOST" ]; then
  echo "Application URL: http://$INGRESS_HOST"
else
  echo "Ingress address not yet assigned. Check later: kubectl get ingress -n $NAMESPACE"
fi
