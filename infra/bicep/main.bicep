targetScope = 'resourceGroup'

@description('Deployment environment name, such as dev or hackathon.')
param environmentName string = 'dev'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short project identifier used in resource naming.')
param projectName string = 'trading'

@description('Optional common tags applied to all supported resources.')
param tags object = {}

@description('Container image for the frontend SPA.')
param frontendImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Container image for the market service.')
param marketServiceImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Container image for the trade service.')
param tradeServiceImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Container image for the settlement service.')
param settlementServiceImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('SQL administrator login.')
param sqlAdminLogin string = 'sqladmintrading'

@secure()
@description('SQL administrator password.')
param sqlAdminPassword string

@description('Auth0 domain used by SPA and APIs.')
param auth0Domain string = 'tenant-placeholder.us.auth0.com'

@description('Auth0 API audience identifier.')
param auth0Audience string = 'https://trading-platform-api'

@description('Auth0 SPA client id.')
param auth0ClientId string = 'replace-me'

var namePrefix = '${projectName}-${environmentName}'
var sqlServerName = 'sql-${namePrefix}-${uniqueString(resourceGroup().id)}'
var sqlDatabaseName = '${projectName}-${environmentName}'
var serviceBusNamespaceName = 'sb-${namePrefix}-${uniqueString(resourceGroup().id)}'
var serviceBusTopicName = 'trading.lifecycle'
// Key Vault names: 3–24 chars, alphanumeric + single hyphens, no consecutive hyphens; kv-{env}-{hash} exceeded 24 for long env names.
var keyVaultName = 'kv-${uniqueString(resourceGroup().id, projectName, environmentName)}'
var containerAppEnvironmentName = 'cae-${namePrefix}'
// Container Apps resource names: max 32 chars; long prefixes (e.g. settlement-service) exceed limit with project+env.
var frontendAppName = 'fe-${namePrefix}'
var marketAppName = 'mkt-${namePrefix}'
var tradeAppName = 'trd-${namePrefix}'
var settlementAppName = 'stl-${namePrefix}'
var acrName = take(replace('acr${projectName}${environmentName}${uniqueString(resourceGroup().id)}', '-', ''), 50)
var sqlConnectionString = 'Server=tcp:${sqlServer.name}.database.windows.net,1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${namePrefix}'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    minimumTlsVersion: '1.2'
  }
}

resource serviceBusTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  name: '${serviceBusNamespace.name}/${serviceBusTopicName}'
  properties: {
    enablePartitioning: true
  }
}

resource orderMatchedSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  name: '${serviceBusTopic.name}/trade-service.on-order-matched'
  properties: {
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
  }
}

resource tradeRecordedSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  name: '${serviceBusTopic.name}/settlement-service.on-trade-recorded'
  properties: {
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
  }
}

resource marketRelaySubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  name: '${serviceBusTopic.name}/market-service.relay-realtime'
  properties: {
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
  }
}

resource serviceBusAuthRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' = {
  name: '${serviceBusNamespace.name}/app-access'
  properties: {
    rights: [
      'Listen'
      'Send'
      'Manage'
    ]
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
}

resource allowAzureServicesFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  name: '${sqlServer.name}/AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: '${sqlServer.name}/${sqlDatabaseName}'
  location: location
  tags: tags
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
  properties: {
    zoneRedundant: false
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForTemplateDeployment: true
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource sqlConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/sql-connection-string'
  properties: {
    value: sqlConnectionString
  }
}

resource serviceBusConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/servicebus-connection-string'
  properties: {
    value: 'Endpoint=sb://${serviceBusNamespace.name}.servicebus.windows.net/;SharedAccessKeyName=app-access;SharedAccessKey=${serviceBusAuthRule.listKeys().primaryKey}'
  }
}

resource appInsightsConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/applicationinsights-connection-string'
  properties: {
    value: monitoring.outputs.applicationInsightsConnectionString
  }
}

resource auth0DomainSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/auth0-domain'
  properties: {
    value: auth0Domain
  }
}

resource auth0AudienceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/auth0-audience'
  properties: {
    value: auth0Audience
  }
}

resource auth0ClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${keyVault.name}/auth0-client-id'
  properties: {
    value: auth0ClientId
  }
}

resource frontendIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-frontend-${namePrefix}'
  location: location
  tags: tags
}

resource marketIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-market-${namePrefix}'
  location: location
  tags: tags
}

resource tradeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-trade-${namePrefix}'
  location: location
  tags: tags
}

resource settlementIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-settlement-${namePrefix}'
  location: location
  tags: tags
}

resource frontendAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, frontendIdentity.id, 'acrpull')
  scope: acr
  properties: {
    principalId: frontendIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource marketAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, marketIdentity.id, 'acrpull')
  scope: acr
  properties: {
    principalId: marketIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource tradeAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, tradeIdentity.id, 'acrpull')
  scope: acr
  properties: {
    principalId: tradeIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource settlementAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, settlementIdentity.id, 'acrpull')
  scope: acr
  properties: {
    principalId: settlementIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource frontendKeyVaultUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, frontendIdentity.id, 'kv-secrets-user')
  scope: keyVault
  properties: {
    principalId: frontendIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
}

resource marketKeyVaultUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, marketIdentity.id, 'kv-secrets-user')
  scope: keyVault
  properties: {
    principalId: marketIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
}

resource tradeKeyVaultUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, tradeIdentity.id, 'kv-secrets-user')
  scope: keyVault
  properties: {
    principalId: tradeIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
}

resource settlementKeyVaultUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, settlementIdentity.id, 'kv-secrets-user')
  scope: keyVault
  properties: {
    principalId: settlementIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: monitoring.outputs.logAnalyticsWorkspaceId
        sharedKey: monitoring.outputs.logAnalyticsSharedKey
      }
    }
  }
}

resource marketService 'Microsoft.App/containerApps@2024-03-01' = {
  name: marketAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${marketIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          identity: marketIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/sql-connection-string'
          identity: marketIdentity.id
        }
        {
          name: 'servicebus-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/servicebus-connection-string'
          identity: marketIdentity.id
        }
        {
          name: 'appinsights-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/applicationinsights-connection-string'
          identity: marketIdentity.id
        }
        {
          name: 'auth0-domain'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-domain'
          identity: marketIdentity.id
        }
        {
          name: 'auth0-audience'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-audience'
          identity: marketIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'market-service'
          image: marketServiceImage
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:8080'
            }
            {
              name: 'ConnectionStrings__Sql'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ConnectionStrings__ServiceBus'
              secretRef: 'servicebus-connection-string'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection-string'
            }
            {
              name: 'Auth0__Domain'
              secretRef: 'auth0-domain'
            }
            {
              name: 'Auth0__Audience'
              secretRef: 'auth0-audience'
            }
            {
              name: 'Messaging__TopicName'
              value: serviceBusTopicName
            }
            {
              name: 'Messaging__Transport'
              value: 'AzureServiceBus'
            }
            {
              name: 'Messaging__SubscriptionName'
              value: 'market-service.relay-realtime'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource tradeService 'Microsoft.App/containerApps@2024-03-01' = {
  name: tradeAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${tradeIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          identity: tradeIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/sql-connection-string'
          identity: tradeIdentity.id
        }
        {
          name: 'servicebus-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/servicebus-connection-string'
          identity: tradeIdentity.id
        }
        {
          name: 'appinsights-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/applicationinsights-connection-string'
          identity: tradeIdentity.id
        }
        {
          name: 'auth0-domain'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-domain'
          identity: tradeIdentity.id
        }
        {
          name: 'auth0-audience'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-audience'
          identity: tradeIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'trade-service'
          image: tradeServiceImage
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:8080'
            }
            {
              name: 'ConnectionStrings__Sql'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ConnectionStrings__ServiceBus'
              secretRef: 'servicebus-connection-string'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection-string'
            }
            {
              name: 'Auth0__Domain'
              secretRef: 'auth0-domain'
            }
            {
              name: 'Auth0__Audience'
              secretRef: 'auth0-audience'
            }
            {
              name: 'Messaging__TopicName'
              value: serviceBusTopicName
            }
            {
              name: 'Messaging__Transport'
              value: 'AzureServiceBus'
            }
            {
              name: 'Messaging__SubscriptionName'
              value: 'trade-service.on-order-matched'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource settlementService 'Microsoft.App/containerApps@2024-03-01' = {
  name: settlementAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${settlementIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          identity: settlementIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/sql-connection-string'
          identity: settlementIdentity.id
        }
        {
          name: 'servicebus-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/servicebus-connection-string'
          identity: settlementIdentity.id
        }
        {
          name: 'appinsights-connection-string'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/applicationinsights-connection-string'
          identity: settlementIdentity.id
        }
        {
          name: 'auth0-domain'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-domain'
          identity: settlementIdentity.id
        }
        {
          name: 'auth0-audience'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-audience'
          identity: settlementIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'settlement-service'
          image: settlementServiceImage
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:8080'
            }
            {
              name: 'ConnectionStrings__Sql'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ConnectionStrings__ServiceBus'
              secretRef: 'servicebus-connection-string'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection-string'
            }
            {
              name: 'Auth0__Domain'
              secretRef: 'auth0-domain'
            }
            {
              name: 'Auth0__Audience'
              secretRef: 'auth0-audience'
            }
            {
              name: 'Messaging__TopicName'
              value: serviceBusTopicName
            }
            {
              name: 'Messaging__Transport'
              value: 'AzureServiceBus'
            }
            {
              name: 'Messaging__SubscriptionName'
              value: 'settlement-service.on-trade-recorded'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource frontendSpa 'Microsoft.App/containerApps@2024-03-01' = {
  name: frontendAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${frontendIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          identity: frontendIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
      }
      secrets: [
        {
          name: 'auth0-domain'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-domain'
          identity: frontendIdentity.id
        }
        {
          name: 'auth0-audience'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-audience'
          identity: frontendIdentity.id
        }
        {
          name: 'auth0-client-id'
          keyVaultUrl: 'https://${keyVault.name}.vault.azure.net/secrets/auth0-client-id'
          identity: frontendIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'frontend-spa'
          image: frontendImage
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'PORT'
              value: '80'
            }
            {
              name: 'VITE_AUTH0_DOMAIN'
              secretRef: 'auth0-domain'
            }
            {
              name: 'VITE_AUTH0_AUDIENCE'
              secretRef: 'auth0-audience'
            }
            {
              name: 'VITE_AUTH0_CLIENT_ID'
              secretRef: 'auth0-client-id'
            }
            {
              name: 'VITE_MARKET_API_BASE_URL'
              value: 'https://${marketService.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'VITE_TRADE_API_BASE_URL'
              value: 'https://${tradeService.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'VITE_SETTLEMENT_API_BASE_URL'
              value: 'https://${settlementService.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

output acrLoginServer string = acr.properties.loginServer
output frontendUrl string = 'https://${frontendSpa.properties.configuration.ingress.fqdn}'
output marketApiUrl string = 'https://${marketService.properties.configuration.ingress.fqdn}'
output tradeApiUrl string = 'https://${tradeService.properties.configuration.ingress.fqdn}'
output settlementApiUrl string = 'https://${settlementService.properties.configuration.ingress.fqdn}'
output keyVaultName string = keyVault.name
output applicationInsightsName string = monitoring.outputs.applicationInsightsName
