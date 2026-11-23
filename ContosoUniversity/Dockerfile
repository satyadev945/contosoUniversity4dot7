FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore \
    -p:DebugType=None -p:DebugSymbols=false -p:UseAppHost=false \
    && find /app/publish -type f \( -name "*.pdb" -o -name "*.tmp" -o -name "*.cs" \) -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Provide the database connection at runtime, e.g. ConnectionStrings__SchoolContext
# App settings are supplied via environment variables (ConfigMaps/Secrets), e.g. SectionName__Key; no web.config appSettings are used.
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
