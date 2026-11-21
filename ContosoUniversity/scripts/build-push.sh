#!/bin/bash
# Build and push the ContosoUniversity image. Run from the repository root (ApSdbzMono).
set -e
set -o pipefail

PROJECT_NAME="ContosoUniversity"
DOCKERFILE="ContosoUniversity/Dockerfile"

sanitize() {
  echo "$1" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._-' '-' | sed 's/^-*//;s/-*$//'
}

IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

read -r -p "Enter image tag [latest]: " RAW_TAG
IMAGE_TAG=$(sanitize "${RAW_TAG:-latest}")
IMAGE_TAG=${IMAGE_TAG:-latest}

echo "Select registry:"
echo "  1. Google Artifact Registry"
echo "  2. Docker Hub"
read -r -p "Choice [1/2]: " REGISTRY_CHOICE

case "$REGISTRY_CHOICE" in
  1)
    read -r -p "GCP project ID: " GCP_PROJECT
    read -r -p "GCP region (e.g. us-central1): " GCP_REGION
    read -r -p "Artifact Registry repository name: " GAR_REPO
    [ -z "$GCP_PROJECT" ] || [ -z "$GCP_REGION" ] || [ -z "$GAR_REPO" ] && { echo "ERROR: all GAR values are required"; exit 1; }
    FULL_IMAGE_NAME="${GCP_REGION}-docker.pkg.dev/${GCP_PROJECT}/${GAR_REPO}/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Authenticating Docker with Artifact Registry..."
    gcloud auth configure-docker "${GCP_REGION}-docker.pkg.dev" --quiet
    ;;
  2)
    read -r -p "Docker Hub username: " DOCKER_USERNAME
    read -r -s -p "Docker Hub password / access token: " DOCKER_PASSWORD
    echo
    [ -z "$DOCKER_USERNAME" ] || [ -z "$DOCKER_PASSWORD" ] && { echo "ERROR: Docker Hub credentials are required"; exit 1; }
    DOCKER_USERNAME=$(echo "$DOCKER_USERNAME" | tr '[:upper:]' '[:lower:]')
    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${IMAGE_NAME}:${IMAGE_TAG}"
    echo "Logging in to Docker Hub..."
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    ;;
  *)
    echo "ERROR: invalid choice"; exit 1 ;;
esac

echo "Building image ${FULL_IMAGE_NAME} ..."
docker build -f "$DOCKERFILE" -t "$FULL_IMAGE_NAME" .

echo "Pushing image ${FULL_IMAGE_NAME} ..."
docker push "$FULL_IMAGE_NAME"

echo "SUCCESS: pushed ${FULL_IMAGE_NAME}"
echo "Use this URI with scripts/deploy-image.sh"
