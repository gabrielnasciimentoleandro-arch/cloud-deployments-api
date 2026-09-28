#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-rg-cloud-deploy-api}"
LOCATION="${LOCATION:-brazilsouth}"
DEPLOYMENT_NAME="${DEPLOYMENT_NAME:-cloud-deploy-infra}"
IMAGE_NAME="${IMAGE_NAME:-cloud-deploy-api}"
IMAGE_TAG="${IMAGE_TAG:-manual-$(date -u +%Y%m%d%H%M%S)}"

command -v az >/dev/null 2>&1 || {
  echo "Azure CLI is required." >&2
  exit 1
}

az account show >/dev/null

az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

az deployment group create \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters location="$LOCATION" \
  --output none

ACR_NAME=$(az deployment group show \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.outputs.registryName.value \
  --output tsv)

ACR_LOGIN_SERVER=$(az deployment group show \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.outputs.registryLoginServer.value \
  --output tsv)

WEBAPP_NAME=$(az deployment group show \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.outputs.webAppName.value \
  --output tsv)

IMAGE="$ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG"

az acr build \
  --registry "$ACR_NAME" \
  --image "$IMAGE_NAME:$IMAGE_TAG" \
  --file Dockerfile \
  .

az webapp config container set \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --container-image-name "$IMAGE" \
  --output none

az webapp restart \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --output none

printf 'Deployment requested successfully.\n'
printf 'API: https://%s.azurewebsites.net/\n' "$WEBAPP_NAME"
printf 'Health: https://%s.azurewebsites.net/healthz\n' "$WEBAPP_NAME"
printf 'Swagger: https://%s.azurewebsites.net/swagger\n' "$WEBAPP_NAME"
