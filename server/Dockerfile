FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
RUN dotnet build -c Release

WORKDIR /app/src/AzureDevOps.InnerSource
RUN dotnet publish -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 44400
ENV ASPNETCORE_URLS=http://+:44400
ENV ASPNETCORE_ENVIRONMENT Production
ENTRYPOINT ["dotnet", "AzureDevOps.InnerSource.dll"]