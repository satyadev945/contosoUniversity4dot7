#!/bin/bash
set -e
set -o pipefail

# Run from the repository root (docker build context is always ".").
# Usage: ContosoUniversity/scripts/build-push.sh

PROJECT_NAME="ContosoUniversity"
DOCKERFILE="ContosoUniversity/Dockerfile"

if [ ! -f "$DOCKERFILE" ]; then
  echo "ERROR: $DOCKERFILE not found. Run this script from the repository root."
  exit 1
fi

sanitize() {
  echo "$1" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._\n' '-' | sed 's/^-*//;s/-*$//'
}

IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

read -r -p "Enter image tag [latest]: " IMAGE_TAG
IMAGE_TAG=$(sanitize "${IMAGE_TAG:-latest}")
IMAGE_TAG=${IMAGE_TAG:-latest}

echo "Select registry type:"
echo "  1. Azure Container Registry (ACR)"
echo "  2. Docker Hub"
read -r -p "Choice [1-2]: " REGISTRY_CHOICE

case "$REGISTRY_CHOICE" in
  1)
    read -r -p "Enter ACR name (without .azurecr.io): " ACR_NAME
    if [ -z "$ACR_NAME" ]; then echo "ERROR: ACR name is required"; exit 1; fi
    ACR_NAME=$(echo "$ACR_NAME" | tr '[:upper:]' '[:lower:]')
    FULL_IMAGE_NAME="${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to ACR ${ACR_NAME}..."
    az acr login --name "$ACR_NAME"
    ;;
  2)
    read -r -p "Enter Docker Hub username: " DOCKER_USERNAME
    if [ -z "$DOCKER_USERNAME" ]; then echo "ERROR: Docker Hub username is required"; exit 1; fi
    read -r -s -p "Enter Docker Hub password/token: " DOCKER_PASSWORD
    echo
    DOCKER_USERNAME=$(echo "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')
    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to Docker Hub..."
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    ;;
  *)
    echo "ERROR: Invalid choice"
    exit 1
    ;;
esac

echo "Building image ${FULL_IMAGE_NAME}..."
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" .

echo "Pushing image ${FULL_IMAGE_NAME}..."
docker push "$FULL_IMAGE_NAME"

echo "Done. Image pushed: ${FULL_IMAGE_NAME}"
echo "Use this URI with scripts/deploy-image.sh"
