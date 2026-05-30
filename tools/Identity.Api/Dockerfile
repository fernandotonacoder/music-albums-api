FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["Directory.Packages.props", "./"]
COPY ["tools/Identity.Api/Identity.Api.csproj", "tools/Identity.Api/"]
COPY ["src/MusicAlbums.ServiceDefaults/MusicAlbums.ServiceDefaults.csproj", "src/MusicAlbums.ServiceDefaults/"]

RUN dotnet restore "tools/Identity.Api/Identity.Api.csproj"

COPY ["tools/Identity.Api/", "tools/Identity.Api/"]
COPY ["src/MusicAlbums.ServiceDefaults/", "src/MusicAlbums.ServiceDefaults/"]
WORKDIR "/app/tools/Identity.Api"
RUN dotnet publish "Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5003
COPY --from=build /app/publish .
RUN useradd -m appuser
USER appuser
ENV ASPNETCORE_URLS=http://+:5003

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
