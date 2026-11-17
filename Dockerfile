# Linux container image for ASP.NET Core (replaces Windows Server Core / IIS .NET Framework 4.7 image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-restore -p:DebugType=None -p:DebugSymbols=false -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Provide at runtime, e.g. ConnectionStrings__SchoolContext="Server=${DB_HOST},${DB_PORT};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True"
ENV ConnectionStrings__SchoolContext=""
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
