FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY DNServer.csproj DNServer/
RUN dotnet restore DNServer/DNServer.csproj
COPY . .
RUN dotnet build DNServer.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DNServer.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DNServer.dll"]
