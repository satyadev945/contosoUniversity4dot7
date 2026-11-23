# Linux container image for ASP.NET Core (replaces Windows Server Core / IIS image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore -p:DebugType=None -p:DebugSymbols=false -p:UseAppHost=false \
    && find /app/publish -name "*.pdb" -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# App settings (formerly web.config appSettings) are overridable via env vars, e.g. AppSettings__webpages:Enabled -> AppSettings__webpages__Enabled (ConfigMap)
ENV AppSettings__webpages__Enabled=false
# Supply at runtime: ConnectionStrings__SchoolContext (or DB_HOST/DB_NAME/DB_USER/DB_PASSWORD)
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
