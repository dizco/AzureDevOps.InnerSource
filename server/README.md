[![.github/workflows/main.yml](https://github.com/dizco/AzureDevOps.InnerSource/actions/workflows/main.yml/badge.svg)](https://github.com/dizco/AzureDevOps.InnerSource/actions/workflows/main.yml) [![Build Status](https://dev.azure.com/gabrielbourgault/Kiosoft/_apis/build/status%2Fdizco.AzureDevOps.InnerSource%20Server?branchName=master)](https://dev.azure.com/gabrielbourgault/Kiosoft/_build/latest?definitionId=24&branchName=master) [![.NET](https://img.shields.io/badge/-7.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)

[![Latest image tag](https://ghcr-badge.egpl.dev/dizco/azuredevops.innersource/latest_tag?trim=major&label=latest%20image&ignore=pr-*)](https://github.com/dizco/AzureDevOps.InnerSource/pkgs/container/azuredevops.innersource) [![Image size](https://ghcr-badge.egpl.dev/dizco/azuredevops.innersource/size?trim=major&label=image%20size&ignore=pr-*)](https://github.com/dizco/AzureDevOps.InnerSource/pkgs/container/azuredevops.innersource)

# AzureDevOps.InnerSource :star2: - Server

## Getting started
1. [Install .NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0), if not already installed.
1. Clone this repository
   ```
   git clone https://github.com/dizco/AzureDevOps.InnerSource.git
   ```

1. Create a new Azure Storage Account
   1. Create a new Table `azuredevopsstars`

1. With Visual Studio, open the `AzureDevOps.InnerSource.sln`
1. Update the `appsettings.json` and `appsettings.Local.json` files according to your needs.
1. Run with Visual Studio by pressing F5 or with command line with:
   ```shell
   dotnet run --project ./src/AzureDevOps.InnerSource/
   ```

### Repository aggregation
1. Run aggregation
   ```shell
   dotnet run --project .\src\AzureDevOps.InnerSource\ aggregate --output-folder ./
   ```

## Deploying
A working dockerfile is provided:
```
docker pull ghcr.io/dizco/azuredevops.innersource:latest
```

For further guidance on how to deploy this service, see [deployment guide](docs/deploy.md).