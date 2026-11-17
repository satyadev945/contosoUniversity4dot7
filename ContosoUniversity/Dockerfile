# Linux container image for ASP.NET Core (replaces Windows Server Core / IIS image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj .
RUN dotnet restore ContosoUniversity.csproj
COPY . .
# Publish without PDB/debug symbols; remove any leftover PDB/XML doc files to keep the final image small
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-restore -p:DebugType=None -p:DebugSymbols=false -p:GenerateDocumentationFile=false \
    && find /app/publish -type f \( -name "*.pdb" -o -name "*.tmp" -o -name "*.cs" -o -name "*.csproj" \) -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Former Views/Web.config appSettings (webpages:Enabled) now supplied as an environment variable; override via ConfigMap
ENV webpages__Enabled=false
# Provide the database connection at runtime, e.g. ConnectionStrings__SchoolContext
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
