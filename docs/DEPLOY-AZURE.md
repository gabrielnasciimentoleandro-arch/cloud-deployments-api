# Deploy no Azure

Este guia publica a imagem da API em um **Azure Container Registry** e executa o container em um **Azure App Service para Linux**.

> **Atenção a custos:** o ACR Basic e o App Service Plan B1 podem gerar cobrança. Exclua o grupo de recursos depois dos testes se não quiser manter o ambiente.

## Recursos criados

O arquivo `infra/main.bicep` provisiona:

- Azure Container Registry Basic com usuário administrador desabilitado;
- Azure App Service Plan Linux B1;
- Azure Web App com HTTPS, TLS 1.2, FTPS desabilitado e health check;
- identidade gerenciada do Web App;
- permissão `AcrPull` da identidade no ACR.

## Pré-requisitos

- assinatura Azure ativa;
- Azure CLI autenticado;
- permissão para criar recursos e atribuições de função;
- repositório clonado localmente.

Confirme a conta selecionada:

```bash
az login
az account show --output table
```

Se necessário, escolha a assinatura:

```bash
az account set --subscription "ID-OU-NOME-DA-ASSINATURA"
```

## Opção 1 — Deploy manual com Azure Container Registry Tasks

Essa opção não exige Docker instalado localmente. O Azure executa o build da imagem.

Na raiz do projeto:

```bash
chmod +x scripts/deploy-azure.sh scripts/delete-azure-resources.sh
RESOURCE_GROUP="rg-cloud-deploy-api" \
LOCATION="brazilsouth" \
./scripts/deploy-azure.sh
```

O script:

1. cria o grupo de recursos;
2. aplica o template Bicep;
3. executa o build remoto no ACR;
4. aponta o Web App para a imagem criada;
5. reinicia a aplicação;
6. exibe as URLs da API, saúde e Swagger.

A propagação da permissão `AcrPull` pode levar alguns minutos. Se a primeira inicialização falhar, aguarde e reinicie o Web App:

```bash
az webapp restart \
  --name "NOME-DO-WEBAPP" \
  --resource-group "rg-cloud-deploy-api"
```

Valide o ambiente real:

```bash
curl --fail "https://NOME-DO-WEBAPP.azurewebsites.net/healthz"
curl --fail "https://NOME-DO-WEBAPP.azurewebsites.net/"
```

## Opção 2 — Deploy pelo GitHub Actions com OIDC

O workflow `.github/workflows/deploy-azure.yml` evita senhas permanentes e só é executado manualmente.

### 1. Crie primeiro a infraestrutura

Execute o deploy manual até a etapa de criação da infraestrutura ou aplique o Bicep diretamente:

```bash
az group create \
  --name "rg-cloud-deploy-api" \
  --location "brazilsouth"

az deployment group create \
  --name "cloud-deploy-infra" \
  --resource-group "rg-cloud-deploy-api" \
  --template-file "infra/main.bicep" \
  --parameters location="brazilsouth"
```

Consulte os nomes gerados:

```bash
az deployment group show \
  --name "cloud-deploy-infra" \
  --resource-group "rg-cloud-deploy-api" \
  --query properties.outputs
```

### 2. Crie a identidade usada pelo GitHub

Substitua `SEU-USUARIO/SEU-REPOSITORIO` pelo caminho real do GitHub:

```bash
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
TENANT_ID=$(az account show --query tenantId --output tsv)
RESOURCE_GROUP="rg-cloud-deploy-api"
GITHUB_REPOSITORY="SEU-USUARIO/SEU-REPOSITORIO"
APP_DISPLAY_NAME="github-cloud-deploy-api"

CLIENT_ID=$(az ad app create \
  --display-name "$APP_DISPLAY_NAME" \
  --query appId \
  --output tsv)
APP_OBJECT_ID=$(az ad app show --id "$CLIENT_ID" --query id --output tsv)
SP_OBJECT_ID=$(az ad sp create --id "$CLIENT_ID" --query id --output tsv)

RG_SCOPE="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP"
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$RG_SCOPE"

cat > /tmp/github-federated-credential.json <<EOF
{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$GITHUB_REPOSITORY:environment:production",
  "description": "GitHub Actions production environment",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

az ad app federated-credential create \
  --id "$APP_OBJECT_ID" \
  --parameters /tmp/github-federated-credential.json

printf 'AZURE_CLIENT_ID=%s\n' "$CLIENT_ID"
printf 'AZURE_TENANT_ID=%s\n' "$TENANT_ID"
printf 'AZURE_SUBSCRIPTION_ID=%s\n' "$SUBSCRIPTION_ID"
```

### 3. Permita que a identidade publique no ACR

```bash
ACR_NAME=$(az deployment group show \
  --name "cloud-deploy-infra" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.outputs.registryName.value \
  --output tsv)

ACR_ID=$(az acr show \
  --name "$ACR_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query id \
  --output tsv)

az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role AcrPush \
  --scope "$ACR_ID"
```

### 4. Configure o GitHub

Em **Settings → Environments**, crie o ambiente `production`.

Em **Settings → Secrets and variables → Actions**, cadastre estes secrets:

- `AZURE_CLIENT_ID`;
- `AZURE_TENANT_ID`;
- `AZURE_SUBSCRIPTION_ID`.

Cadastre estas variables:

- `AZURE_RESOURCE_GROUP` — `rg-cloud-deploy-api`;
- `ACR_NAME` — nome retornado pelo Bicep;
- `ACR_LOGIN_SERVER` — por exemplo, `nome.azurecr.io`;
- `AZURE_WEBAPP_NAME` — nome retornado pelo Bicep.

Depois abra **Actions → Deploy to Azure → Run workflow**.

## Logs e diagnóstico

```bash
az webapp log config \
  --name "NOME-DO-WEBAPP" \
  --resource-group "rg-cloud-deploy-api" \
  --docker-container-logging filesystem

az webapp log tail \
  --name "NOME-DO-WEBAPP" \
  --resource-group "rg-cloud-deploy-api"
```

Nunca publique secrets, tokens, credenciais do ACR ou arquivos de perfil de publicação.

## Excluir os recursos

```bash
RESOURCE_GROUP="rg-cloud-deploy-api" ./scripts/delete-azure-resources.sh
```

Ou diretamente:

```bash
az group delete --name "rg-cloud-deploy-api" --yes
```
