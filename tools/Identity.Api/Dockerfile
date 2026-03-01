FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["Identity.Api.csproj", "./"]
RUN dotnet restore "Identity.Api.csproj"

COPY ["Contracts/", "Contracts/"]
COPY ["Controllers/", "Controllers/"]
COPY ["Properties/", "Properties/"]
COPY ["Program.cs", "./"]
RUN dotnet publish "Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5003
COPY --from=build /app/publish .
RUN useradd -m appuser
USER appuser
ENV ASPNETCORE_URLS=http://+:5003

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
