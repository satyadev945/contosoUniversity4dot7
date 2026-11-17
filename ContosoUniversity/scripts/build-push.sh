#!/bin/bash
# Build and push the ContosoUniversity image. Run from the repository root (AppDBMono_CS1).
set -e
set -o pipefail

echo "=== ContosoUniversity - Build & Push ==="

read -r -p "Project name [contosouniversity]: " PROJECT_NAME
PROJECT_NAME=${PROJECT_NAME:-contosouniversity}
IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')
[ -z "$IMAGE_NAME" ] && IMAGE_NAME="contosouniversity"

read -r -p "Image tag [latest]: " IMAGE_TAG
IMAGE_TAG=$(echo "${IMAGE_TAG:-latest}" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._' '-' | sed 's/^-*//;s/-*$//')
[ -z "$IMAGE_TAG" ] && IMAGE_TAG="latest"

echo "Select registry:"
echo "  1. AWS ECR"
echo "  2. Docker Hub"
read -r -p "Choice [1/2]: " REGISTRY_CHOICE

DOCKERFILE="ContosoUniversity/Dockerfile"
if [ ! -f "$DOCKERFILE" ]; then
  echo "ERROR: $DOCKERFILE not found. Run this script from the repository root."
  exit 1
fi

case "$REGISTRY_CHOICE" in
  1)
    read -r -p "AWS region (e.g. us-east-1): " AWS_REGION
    read -r -p "AWS account ID (12 digits): " AWS_ACCOUNT_ID
    read -r -p "ECR repository name [$IMAGE_NAME]: " ECR_REPO
    ECR_REPO=${ECR_REPO:-$IMAGE_NAME}
    if [ -z "$AWS_REGION" ] || [ -z "$AWS_ACCOUNT_ID" ]; then
      echo "ERROR: AWS region and account ID are required."; exit 1
    fi
    REGISTRY_URL="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    FULL_IMAGE_NAME="${REGISTRY_URL}/${ECR_REPO}:${IMAGE_TAG}"

    echo "Logging in to ECR..."
    aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$REGISTRY_URL"

    echo "Ensuring ECR repository exists..."
    aws ecr describe-repositories --repository-names "$ECR_REPO" --region "$AWS_REGION" >/dev/null 2>&1 || \
      aws ecr create-repository --repository-name "$ECR_REPO" --region "$AWS_REGION"
    ;;
  2)
    read -r -p "Docker Hub username: " DOCKER_USERNAME
    read -r -s -p "Docker Hub password / access token: " DOCKER_PASSWORD
    echo
    read -r -p "Repository name [$IMAGE_NAME]: " DOCKER_REPO
    DOCKER_REPO=${DOCKER_REPO:-$IMAGE_NAME}
    if [ -z "$DOCKER_USERNAME" ] || [ -z "$DOCKER_PASSWORD" ]; then
      echo "ERROR: Docker Hub credentials are required."; exit 1
    fi
    FULL_IMAGE_NAME="$(echo "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')/${DOCKER_REPO}:${IMAGE_TAG}"

    echo "Logging in to Docker Hub..."
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    ;;
  *)
    echo "ERROR: invalid choice."; exit 1
    ;;
esac

echo "Building image $FULL_IMAGE_NAME ..."
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" .

echo "Pushing image $FULL_IMAGE_NAME ..."
docker push "$FULL_IMAGE_NAME"

echo "SUCCESS: pushed $FULL_IMAGE_NAME"
echo "Use this URI with scripts/deploy-image.sh"
