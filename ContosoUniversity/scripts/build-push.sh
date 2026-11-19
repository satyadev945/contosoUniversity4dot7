#!/bin/bash
set -e
set -o pipefail

# Resolve paths independent of the current directory.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODULE_DIR="$(dirname "$SCRIPT_DIR")"
REPO_ROOT="$(dirname "$MODULE_DIR")"
DOCKERFILE="$MODULE_DIR/Dockerfile"

PROJECT_NAME="ContosoUniversity"
IMAGE_NAME=$(printf '%s' "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

echo "=== ContosoUniversity - Build and Push ==="

read -r -p "Enter image tag [latest]: " IMAGE_TAG
IMAGE_TAG=$(printf '%s' "$IMAGE_TAG" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._-' '-' | sed 's/^-*//;s/-*$//')
IMAGE_TAG=${IMAGE_TAG:-latest}

echo "Select registry type:"
echo "  1. Azure Container Registry (ACR)"
echo "  2. Docker Hub"
read -r -p "Enter choice [1-2]: " REGISTRY_CHOICE

case "$REGISTRY_CHOICE" in
  1)
    read -r -p "Enter ACR name (without .azurecr.io): " ACR_NAME
    [ -n "$ACR_NAME" ] || { echo "ERROR: ACR name is required"; exit 1; }
    ACR_NAME=$(printf '%s' "$ACR_NAME" | tr '[:upper:]' '[:lower:]')
    FULL_IMAGE_NAME="${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to Azure Container Registry..."
    az acr login --name "$ACR_NAME"
    ;;
  2)
    read -r -p "Enter Docker Hub username: " DOCKER_USERNAME
    [ -n "$DOCKER_USERNAME" ] || { echo "ERROR: Docker Hub username is required"; exit 1; }
    read -r -s -p "Enter Docker Hub password or access token: " DOCKER_PASSWORD
    echo
    DOCKER_USERNAME=$(printf '%s' "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')
    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to Docker Hub..."
    printf '%s' "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    ;;
  *)
    echo "ERROR: Invalid choice"
    exit 1
    ;;
esac

echo "Building image: $FULL_IMAGE_NAME"
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" "$REPO_ROOT"

echo "Pushing image: $FULL_IMAGE_NAME"
docker push "$FULL_IMAGE_NAME"

echo "Done. Image pushed: $FULL_IMAGE_NAME"
echo "Use this URI with scripts/deploy-image.sh"
