FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["DNServer/DNServer.csproj", "DNServer/"]
COPY ["ARSoft.Tools.Net/ARSoft.Tools.Net/ARSoft.Tools.Net.csproj", "ARSoft.Tools.Net/ARSoft.Tools.Net/"]
RUN dotnet restore "DNServer/DNServer.csproj"
COPY . .
WORKDIR "/src/DNServer"
RUN dotnet build "DNServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "DNServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DNServer.dll"]