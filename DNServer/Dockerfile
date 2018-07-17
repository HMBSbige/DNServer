FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY DNServer.csproj DNServer/
RUN dotnet restore DNServer.csproj
COPY . .
WORKDIR /src/DNServer
RUN dotnet build DNServer.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DNServer.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DNServer.dll"]
