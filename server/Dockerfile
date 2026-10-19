FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TamQuoc3D.Server.csproj ./
RUN dotnet restore TamQuoc3D.Server.csproj
COPY . ./
RUN dotnet publish TamQuoc3D.Server.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
EXPOSE 8000
HEALTHCHECK --interval=5s --timeout=3s --retries=5 \
    CMD curl --fail --silent http://localhost:8000/health || exit 1
ENTRYPOINT ["dotnet", "TamQuoc3D.Server.dll"]
