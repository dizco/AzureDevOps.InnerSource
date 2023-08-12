# Contributing

For any major change, please open an issue before submitting a PR. For smaller issues, you are very welcome to open a PR directly.

The project combines an Azure DevOps extension built in ReactJS with a [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) server.

## Install
Follow the instructions in the README.

## Azure DevOps Extension

The extension was started from this sample: https://github.com/microsoft/azure-devops-extension-sample
This sample is also interesting: https://github.com/microsoft/azure-devops-extension-hot-reload-and-debug

Icon list: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font
https://uifabricicons.azurewebsites.net/


Extension manifest: https://learn.microsoft.com/en-us/azure/devops/extend/develop/manifest?view=azure-devops#scopes


There is an issue with unit tests, that has the same symptoms as this: https://github.com/nrwl/nx/issues/812

For further instructions, consult the [extension readme](./extension/README.md).

## .NET Server

For further instructions, consult the [server readme](./server/README.md).
