#!/bin/bash
# Build and push the ContosoUniversity image to AWS ECR or Docker Hub.
# Run from anywhere; the Docker build context is the ContosoUniversity module directory.
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODULE_DIR="$(dirname "$SCRIPT_DIR")"
PROJECT_NAME="ContosoUniversity"
IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

echo "=== ContosoUniversity build and push ==="
echo "Select registry type:"
echo "  1. AWS ECR"
echo "  2. Docker Hub"
read -r -p "Enter choice (1 or 2): " REGISTRY_CHOICE

read -r -p "Enter image tag (default: latest): " RAW_TAG
IMAGE_TAG=$(echo "$RAW_TAG" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._-' '-' | sed 's/^[-.]*//;s/[-.]*$//')
if [ -z "$IMAGE_TAG" ]; then
  IMAGE_TAG="latest"
fi
echo "Using image name '$IMAGE_NAME' and tag '$IMAGE_TAG'"

BUILD_ARGS=""

if [ "$REGISTRY_CHOICE" = "1" ]; then
  read -r -p "Enter AWS region (e.g., us-east-1): " AWS_REGION
  if [ -z "$AWS_REGION" ]; then echo "AWS region is required"; exit 1; fi
  ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
  REGISTRY_URL="${ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
  ECR_REPO="$IMAGE_NAME"
  FULL_IMAGE_NAME="${REGISTRY_URL}/${ECR_REPO}:${IMAGE_TAG}"

  echo "Logging in to ECR $REGISTRY_URL ..."
  aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$REGISTRY_URL"

  echo "Ensuring ECR repository '$ECR_REPO' exists ..."
  aws ecr describe-repositories --repository-names "$ECR_REPO" --region "$AWS_REGION" >/dev/null 2>&1 || \
    aws ecr create-repository --repository-name "$ECR_REPO" --region "$AWS_REGION"

  read -r -p "ECR pull-through cache prefix for MCR base images (leave empty to use mcr.microsoft.com): " CACHE_PREFIX
  if [ -n "$CACHE_PREFIX" ]; then
    BUILD_ARGS="--build-arg BASE_REGISTRY=${REGISTRY_URL}/${CACHE_PREFIX}"
  fi
elif [ "$REGISTRY_CHOICE" = "2" ]; then
  read -r -p "Enter Docker Hub username: " DOCKER_USERNAME
  read -r -s -p "Enter Docker Hub password or access token: " DOCKER_PASSWORD
  echo ""
  if [ -z "$DOCKER_USERNAME" ]; then echo "Docker Hub username is required"; exit 1; fi
  DOCKER_USER_LC=$(echo "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')
  FULL_IMAGE_NAME="${DOCKER_USER_LC}/${IMAGE_NAME}:${IMAGE_TAG}"
  echo "Logging in to Docker Hub ..."
  echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
else
  echo "Invalid choice. Enter 1 or 2."
  exit 1
fi

echo "Building image $FULL_IMAGE_NAME ..."
# shellcheck disable=SC2086
docker build -f "$MODULE_DIR/Dockerfile" $BUILD_ARGS -t "$FULL_IMAGE_NAME" "$MODULE_DIR"

echo "Pushing image $FULL_IMAGE_NAME ..."
docker push "$FULL_IMAGE_NAME"

echo ""
echo "Build and push completed successfully."
echo "Image URI: $FULL_IMAGE_NAME"
echo "Use this URI when running scripts/deploy-image.sh"
