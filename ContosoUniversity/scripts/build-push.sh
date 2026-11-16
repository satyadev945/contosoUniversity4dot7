#!/bin/bash
set -e

# Run from repository root: ./ContosoUniversity/scripts/build-push.sh
PROJECT_NAME="ContosoUniversity"
DOCKERFILE="ContosoUniversity/Dockerfile"

IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

read -p "Enter image tag [latest]: " IMAGE_TAG
IMAGE_TAG=$(echo "${IMAGE_TAG:-latest}" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._' '-' | sed 's/^-*//;s/-*$//')
IMAGE_TAG=${IMAGE_TAG:-latest}

echo "Select registry:"
echo "  1. AWS ECR"
echo "  2. Docker Hub"
read -p "Choice [1/2]: " REGISTRY_CHOICE

case "$REGISTRY_CHOICE" in
  1)
    read -p "AWS region (e.g. us-east-1): " AWS_REGION
    read -p "AWS account ID: " AWS_ACCOUNT_ID
    read -p "ECR repository name [$IMAGE_NAME]: " ECR_REPO
    ECR_REPO=${ECR_REPO:-$IMAGE_NAME}
    [ -z "$AWS_REGION" ] || [ -z "$AWS_ACCOUNT_ID" ] && { echo "Region and account ID are required"; exit 1; }
    REGISTRY_URL="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    FULL_IMAGE_NAME="${REGISTRY_URL}/${ECR_REPO}:${IMAGE_TAG}"
    echo "Logging in to ECR..."
    aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$REGISTRY_URL"
    aws ecr describe-repositories --repository-names $ECR_REPO --region $AWS_REGION >/dev/null 2>&1 || aws ecr create-repository --repository-name $ECR_REPO --region $AWS_REGION
    ;;
  2)
    read -p "Docker Hub username: " DOCKER_USERNAME
    read -s -p "Docker Hub password/token: " DOCKER_PASSWORD
    echo
    read -p "Repository name [$IMAGE_NAME]: " DOCKER_REPO
    DOCKER_REPO=${DOCKER_REPO:-$IMAGE_NAME}
    [ -z "$DOCKER_USERNAME" ] && { echo "Username is required"; exit 1; }
    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${DOCKER_REPO}:${IMAGE_TAG}"
    echo "Logging in to Docker Hub..."
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    ;;
  *)
    echo "Invalid choice"; exit 1 ;;
esac

echo "Building image $FULL_IMAGE_NAME ..."
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" .

echo "Pushing image $FULL_IMAGE_NAME ..."
docker push "$FULL_IMAGE_NAME"

echo "Done. Image URI: $FULL_IMAGE_NAME"
