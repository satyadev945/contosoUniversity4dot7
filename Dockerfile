# Linux-based container image for ASP.NET Core (replaces Windows Server Core / IIS hosting)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false
RUN find /app/publish -name '*.pdb' -delete \
 && find /app/publish -name '*.tmp' -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Supply at runtime, e.g. -e ConnectionStrings__SchoolContext="Server=${DB_HOST};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True"
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
