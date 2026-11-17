#!/bin/bash
# Build and push the ContosoUniversity image. Run from the repository root.
set -e

PROJECT_NAME="ContosoUniversity"
DOCKERFILE="ContosoUniversity/Dockerfile"

IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

echo "=== ContosoUniversity build & push ==="
echo "Select registry:"
echo "  1. Google Artifact Registry"
echo "  2. Docker Hub"
read -r -p "Enter choice [1-2]: " REGISTRY_CHOICE

read -r -p "Enter image tag [latest]: " IMAGE_TAG
IMAGE_TAG=$(echo "$IMAGE_TAG" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._' '-' | sed 's/^-*//;s/-*$//')
IMAGE_TAG=${IMAGE_TAG:-latest}

case "$REGISTRY_CHOICE" in
  1)
    read -r -p "Enter GCP project ID: " GCP_PROJECT
    read -r -p "Enter GCP region (e.g. us-central1): " GCP_REGION
    read -r -p "Enter Artifact Registry repository name: " GAR_REPO
    if [ -z "$GCP_PROJECT" ] || [ -z "$GCP_REGION" ] || [ -z "$GAR_REPO" ]; then
      echo "ERROR: GCP project, region and repository are required"; exit 1
    fi
    echo "Configuring Docker authentication for ${GCP_REGION}-docker.pkg.dev ..."
    gcloud auth configure-docker "${GCP_REGION}-docker.pkg.dev" --quiet
    FULL_IMAGE_NAME="${GCP_REGION}-docker.pkg.dev/${GCP_PROJECT}/${GAR_REPO}/${IMAGE_NAME}:${IMAGE_TAG}"
    ;;
  2)
    read -r -p "Enter Docker Hub username: " DOCKER_USERNAME
    read -r -s -p "Enter Docker Hub password/token: " DOCKER_PASSWORD
    echo
    if [ -z "$DOCKER_USERNAME" ] || [ -z "$DOCKER_PASSWORD" ]; then
      echo "ERROR: Docker Hub credentials are required"; exit 1
    fi
    DOCKER_USERNAME=$(echo "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"
    ;;
  *)
    echo "ERROR: Invalid choice"; exit 1
    ;;
esac

echo "Building image: $FULL_IMAGE_NAME"
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" .

echo "Pushing image: $FULL_IMAGE_NAME"
docker push "$FULL_IMAGE_NAME"

echo "SUCCESS: Image pushed -> $FULL_IMAGE_NAME"
echo "Use this URI with scripts/deploy-image.sh"
