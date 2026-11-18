# Build context: the ContosoUniversity project directory
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore /p:DebugType=None /p:DebugSymbols=false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Replaces Views/Web.config appSettings (webpages:Enabled=false); override at runtime via ConfigMap/env (ASP.NET Core maps '__' to ':')
ENV webpages__Enabled=false
ENV ASPNETCORE_URLS=http://+:8080
# Provide the database connection at runtime, e.g. -e SCHOOL_DB_CONNECTION="Server=${DB_HOST};Database=ContosoUniversity;User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True"
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
