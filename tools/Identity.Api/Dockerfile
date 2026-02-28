FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["tools/Identity.Api/Identity.Api.csproj", "tools/Identity.Api/"]
RUN dotnet restore "tools/Identity.Api/Identity.Api.csproj"

COPY . .
WORKDIR "/app/tools/Identity.Api"
RUN dotnet publish "Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5003
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5003

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
