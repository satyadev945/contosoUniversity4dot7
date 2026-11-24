# Linux container image for ASP.NET Core (replaces Windows Server Core / IIS image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore ContosoUniversity.csproj
COPY . .
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-restore /p:DebugType=None /p:DebugSymbols=false
# Remove any residual PDB/XML doc files from the publish output
RUN find /app/publish -type f \( -name '*.pdb' -o -name '*.xml' -o -name '*.cs' \) -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Former web.config appSettings key "webpages:Enabled" now supplied as an environment variable (override via K8s ConfigMap)
ENV webpages__Enabled=false
# Supply the database connection string at runtime, e.g. -e DB_CONNECTION_STRING=...
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
