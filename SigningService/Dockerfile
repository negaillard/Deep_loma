# Сборка из корня репозитория: docker build -f SigningService/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SigningService/SigningService.csproj", "SigningService/"]
COPY ["NewDiplom/Contracts.csproj", "NewDiplom/"]
COPY ["Logic/Logic.csproj", "Logic/"]
COPY ["Storage/Storage.csproj", "Storage/"]
COPY ["Models/Models.csproj", "Models/"]
COPY ["FileStorage/FileStorage.csproj", "FileStorage/"]
COPY ["MessageContracts/MessageContracts.csproj", "MessageContracts/"]
COPY ["AppLogging/AppLogging.csproj", "AppLogging/"]
COPY ["Nugets/", "Nugets/"]
COPY ["nuget.config", "./"]
RUN dotnet restore "./SigningService/SigningService.csproj"
COPY . .
RUN dotnet publish "./SigningService/SigningService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SigningService.dll"]
