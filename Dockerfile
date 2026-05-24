# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Central Package Management config — csproj files reference packages without versions
COPY ["Directory.Packages.props", "./"]

# Copy project files for restore (optimizes Docker layer caching)
COPY ["src/MusicAlbums.Api/MusicAlbums.Api.csproj", "src/MusicAlbums.Api/"]
COPY ["src/MusicAlbums.Application/MusicAlbums.Application.csproj", "src/MusicAlbums.Application/"]
COPY ["src/MusicAlbums.Contracts/MusicAlbums.Contracts.csproj", "src/MusicAlbums.Contracts/"]
COPY ["MusicAlbumsApi.ServiceDefaults/MusicAlbumsApi.ServiceDefaults.csproj", "MusicAlbumsApi.ServiceDefaults/"]
RUN dotnet restore "src/MusicAlbums.Api/MusicAlbums.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/app/src/MusicAlbums.Api"
RUN dotnet publish "MusicAlbums.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

# Ensure .NET listens on port 8080
ENV ASPNETCORE_URLS=http://+:8080

RUN useradd -m appuser
USER appuser

ENTRYPOINT ["dotnet", "MusicAlbums.Api.dll"]