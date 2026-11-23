#!/bin/bash
set -e
set -o pipefail

PROJECT_NAME="ContosoUniversity"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKERFILE="$SCRIPT_DIR/../Dockerfile"
BUILD_CONTEXT="$SCRIPT_DIR/../.."   # repository root (contains ContosoUniversity.sln)

IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

echo "=== ContosoUniversity - Build and Push ==="

read -r -p "Enter image tag [latest]: " IMAGE_TAG
IMAGE_TAG=$(echo "$IMAGE_TAG" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._' '-' | sed 's/^-*//;s/-*$//')
if [ -z "$IMAGE_TAG" ]; then IMAGE_TAG="latest"; fi

echo "Select registry type:"
echo "  1. Azure Container Registry (ACR)"
echo "  2. Docker Hub"
read -r -p "Choice [1/2]: " REGISTRY_CHOICE

case "$REGISTRY_CHOICE" in
  1)
    read -r -p "Enter ACR name (without .azurecr.io): " ACR_NAME
    ACR_NAME=$(echo "$ACR_NAME" | tr '[:upper:]' '[:lower:]')
    if [ -z "$ACR_NAME" ]; then echo "ERROR: ACR name is required"; exit 1; fi
    FULL_IMAGE_NAME="${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to ACR..."
    az acr login --name "$ACR_NAME"
    ;;
  2)
    read -r -p "Enter Docker Hub username: " DOCKER_USERNAME
    read -r -s -p "Enter Docker Hub password or access token: " DOCKER_PASSWORD
    echo
    if [ -z "$DOCKER_USERNAME" ] || [ -z "$DOCKER_PASSWORD" ]; then echo "ERROR: credentials are required"; exit 1; fi
    DOCKER_USERNAME=$(echo "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')
    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to Docker Hub..."
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    ;;
  *)
    echo "ERROR: invalid registry choice"; exit 1
    ;;
esac

echo "Building image: $FULL_IMAGE_NAME"
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" "$BUILD_CONTEXT"

echo "Pushing image: $FULL_IMAGE_NAME"
docker push "$FULL_IMAGE_NAME"

echo "SUCCESS: pushed $FULL_IMAGE_NAME"
echo "Use this image URI with scripts/deploy-image.sh"
