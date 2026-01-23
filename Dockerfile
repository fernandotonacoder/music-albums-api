# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files for restore (optimizes Docker layer caching)
COPY ["src/MusicAlbums.Api/MusicAlbums.Api.csproj", "src/MusicAlbums.Api/"]
COPY ["src/MusicAlbums.Application/MusicAlbums.Application.csproj", "src/MusicAlbums.Application/"]
COPY ["src/MusicAlbums.Contracts/MusicAlbums.Contracts.csproj", "src/MusicAlbums.Contracts/"]
RUN dotnet restore "src/MusicAlbums.Api/MusicAlbums.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/app/src/MusicAlbums.Api"
RUN dotnet publish "MusicAlbums.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .

# Ensure .NET listens on port 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MusicAlbums.Api.dll"]