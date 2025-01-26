param location string = resourceGroup().location
param storageAccountName string = uniqueString(resourceGroup().id, 'weatherimages')
param functionAppName string = uniqueString(resourceGroup().id, 'weatherfunctions')

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2021-09-01' = {
  parent: storageAccount
  name: 'default'
}

resource weatherJobsQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  parent: queueService
  name: 'weather-jobs'
}

resource imageJobsQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  parent: queueService
  name: 'image-jobs'
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-09-01' = {
  parent: storageAccount
  name: 'default'
}

resource weatherImagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: blobService
  name: 'weather-images'
  properties: {
    publicAccess: 'Blob'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${functionAppName}-appinsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    WorkspaceResourceId: null
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    tier: 'Dynamic'
    name: 'Y1'
  }
  kind: 'FunctionApp'
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, '2021-09-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureWebJobsQueueStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, '2021-09-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'unsplashApiKey'
          value: 'P5nC6hftNG3R7ZRA0-o80VuE8WEC20mjbWoDh1eVgs4'
        }
      ]
    }
    httpsOnly: true
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output functionAppUrl string = 'https://${functionAppName}.azurewebsites.net'
output storageAccountName string = storageAccount.name
output queueNames array = [weatherJobsQueue.name, imageJobsQueue.name]
output containerName string = weatherImagesContainer.name
