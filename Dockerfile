FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GoodStuff.ApiGateway/GoodStuff.ApiGateway.csproj", "GoodStuff.ApiGateway/"]
RUN dotnet restore "GoodStuff.ApiGateway/GoodStuff.ApiGateway.csproj"
COPY . .
WORKDIR "/src/GoodStuff.ApiGateway"
RUN dotnet publish "./GoodStuff.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER $APP_UID
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GoodStuff.ApiGateway.dll"]