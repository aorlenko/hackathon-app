param location string
param namePrefix string
param tags object = {}
param alertEmailAddress string = ''

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-law'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: '${namePrefix}-appi'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
  }
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = if (!empty(alertEmailAddress)) {
  name: '${namePrefix}-ops'
  location: 'global'
  tags: tags
  properties: {
    enabled: true
    groupShortName: 'tradeops'
    emailReceivers: [
      {
        name: 'primary'
        emailAddress: alertEmailAddress
        useCommonAlertSchema: true
      }
    ]
  }
}

resource lifecycleFailuresAlert 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = {
  name: '${namePrefix}-lifecycle-failures'
  location: location
  tags: tags
  properties: {
    description: 'Alerts when lifecycle services report failed requests in the last five minutes.'
    displayName: 'Trading lifecycle failures'
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    scopes: [
      workspace.id
    ]
    severity: 2
    criteria: {
      allOf: [
        {
          query: '''
requests
| where cloud_RoleName in~ ("market-service", "trade-service", "settlement-service")
| where success == false
| summarize failures = count()
'''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 5
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: !empty(alertEmailAddress) ? [
        actionGroup.id
      ] : []
    }
  }
}

output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output applicationInsightsName string = applicationInsights.name
output logAnalyticsWorkspaceId string = workspace.properties.customerId
output logAnalyticsSharedKey string = listKeys(workspace.id, '2023-09-01').primarySharedKey
