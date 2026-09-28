targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Globally unique Azure Web App name.')
param webAppName string = 'clouddeploy-${uniqueString(resourceGroup().id)}'

@description('Globally unique Azure Container Registry name using only lowercase letters and numbers.')
param registryName string = 'acrclouddeploy${uniqueString(resourceGroup().id)}'

@description('Linux App Service plan name.')
param appServicePlanName string = 'plan-cloud-deploy'

@description('App Service plan SKU. B1 supports custom Linux containers and incurs cost.')
param appServicePlanSku string = 'B1'

@description('Public bootstrap image replaced by the first deployment.')
param bootstrapImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServicePlanSku
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOCKER|${bootstrapImage}'
      acrUseManagedIdentityCreds: true
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      healthCheckPath: '/healthz'
      appSettings: [
        {
          name: 'WEBSITES_PORT'
          value: '8080'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'Swagger__Enabled'
          value: 'true'
        }
      ]
    }
  }
}

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, webApp.id, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

output registryName string = registry.name
output registryLoginServer string = registry.properties.loginServer
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
