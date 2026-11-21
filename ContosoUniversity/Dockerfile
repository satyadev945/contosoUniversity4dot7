# Linux container image for ASP.NET Core (replaces Windows/IIS .NET Framework hosting)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false
RUN find /app/publish -name "*.pdb" -delete && find /app/publish -name "*.xml" -not -name "*.config" -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Provide the database connection at runtime, e.g. SCHOOL_DB_CONNECTION or ConnectionStrings__SchoolContext
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
