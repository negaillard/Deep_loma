# Сборка из корня репозитория: docker build -f DemoApp/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DemoApp/DemoApp.csproj", "DemoApp/"]
COPY ["NewDiplom/Contracts.csproj", "NewDiplom/"]
COPY ["Models/Models.csproj", "Models/"]
COPY ["Nugets/", "Nugets/"]
COPY ["nuget.config", "./"]
RUN dotnet restore "./DemoApp/DemoApp.csproj"
COPY . .
RUN dotnet publish "./DemoApp/DemoApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DemoApp.dll"]
