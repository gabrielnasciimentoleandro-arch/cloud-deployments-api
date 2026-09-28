#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-rg-cloud-deploy-api}"

command -v az >/dev/null 2>&1 || {
  echo "Azure CLI is required." >&2
  exit 1
}

printf 'This permanently deletes the Azure resource group: %s\n' "$RESOURCE_GROUP"
read -r -p 'Type DELETE to continue: ' confirmation

if [[ "$confirmation" != "DELETE" ]]; then
  echo 'Deletion cancelled.'
  exit 0
fi

az group delete --name "$RESOURCE_GROUP" --yes
