$resourceGroupName = "weather-image-generator-rg"
$location = "westeurope"
$bicepFile = "./deployment/main.bicep"
$functionAppName = "weather-image-generator-fn"
$storageAccountName = "weatherimages" + $(Get-Random -Minimum 1000 -Maximum 9999)
$publishFolder = "./bin/Release/net7.0/publish"

az login --output none

az group create --name $resourceGroupName --location $location --output none

az deployment group create `
    --resource-group $resourceGroupName `
    --template-file $bicepFile `
    --parameters functionAppName=$functionAppName storageAccountName=$storageAccountName location=$location

dotnet publish WeatherImageGenerator.csproj --configuration Release --output $publishFolder

$zipFilePath = "$publishFolder.zip"
if (Test-Path $zipFilePath) { Remove-Item $zipFilePath }
Compress-Archive -Path $publishFolder\* -DestinationPath $zipFilePath

az functionapp deployment source config-zip `
    --resource-group $resourceGroupName `
    --name $functionAppName `
    --src $zipFilePath

$functionAppUrl = "https://${functionAppName}.azurewebsites.net"
Write-Host "Deployment complete! Your Function App is available at: $functionAppUrl"
