# Linux container for ASP.NET Core (net8.0). Build context: repository root.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=builder --chown=$APP_UID:$APP_UID /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC
# Supply at runtime: ConnectionStrings__SchoolContext="Host=<host>;Database=<db>;Username=<user>;Password=<pwd>"
EXPOSE 8080
# Non-root user (built into the official .NET 8 images)
USER $APP_UID
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
