#!/bin/bash
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_NAME="ContosoUniversity"
IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

echo "=== ContosoUniversity build and push ==="
read -r -p "Image tag [latest]: " RAW_TAG
IMAGE_TAG=$(echo "${RAW_TAG:-latest}" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._' '-' | sed 's/^-*//;s/-*$//')
IMAGE_TAG=${IMAGE_TAG:-latest}
echo "Using image tag: $IMAGE_TAG"

echo "Select registry: 1. AWS ECR  2. Docker Hub"
read -r -p "Choice [1/2]: " REGISTRY_CHOICE

if [ "$REGISTRY_CHOICE" = "1" ]; then
  read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
  read -r -p "AWS account ID: " AWS_ACCOUNT_ID
  [ -n "$AWS_REGION" ] && [ -n "$AWS_ACCOUNT_ID" ] || { echo "Region and account ID are required"; exit 1; }
  read -r -p "ECR repository name [$IMAGE_NAME]: " ECR_REPO
  ECR_REPO=${ECR_REPO:-$IMAGE_NAME}
  REGISTRY_URL="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com"
  FULL_IMAGE_NAME="$REGISTRY_URL/$ECR_REPO:$IMAGE_TAG"
  echo "Logging in to ECR..."
  aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$REGISTRY_URL"
  aws ecr describe-repositories --repository-names "$ECR_REPO" --region "$AWS_REGION" >/dev/null 2>&1 || aws ecr create-repository --repository-name "$ECR_REPO" --region "$AWS_REGION"
elif [ "$REGISTRY_CHOICE" = "2" ]; then
  read -r -p "Docker Hub username: " DOCKER_USERNAME
  read -r -s -p "Docker Hub password/token: " DOCKER_PASSWORD
  echo
  [ -n "$DOCKER_USERNAME" ] || { echo "Username is required"; exit 1; }
  read -r -p "Repository name [$IMAGE_NAME]: " DH_REPO
  DH_REPO=${DH_REPO:-$IMAGE_NAME}
  FULL_IMAGE_NAME="$DOCKER_USERNAME/$DH_REPO:$IMAGE_TAG"
  echo "Logging in to Docker Hub..."
  echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
else
  echo "Invalid registry choice"; exit 1
fi

echo "Building image $FULL_IMAGE_NAME ..."
docker build -f "$REPO_ROOT/ContosoUniversity/Dockerfile" -t "$FULL_IMAGE_NAME" "$REPO_ROOT"

echo "Pushing image $FULL_IMAGE_NAME ..."
docker push "$FULL_IMAGE_NAME"

echo "Done. Image pushed: $FULL_IMAGE_NAME"
echo "Use this URI with scripts/deploy-image.sh"
