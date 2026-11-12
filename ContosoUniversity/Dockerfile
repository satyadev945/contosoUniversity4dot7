# Build context: the ContosoUniversity project directory
# docker build -t contosouniversity .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore ContosoUniversity.csproj
COPY . .
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false \
    && find /app/publish -type f \( -name '*.pdb' -o -name '*.cs' -o -name '*.tmp' \) -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
# Supply at runtime, e.g. ConnectionStrings__SchoolContext="Server=${DB_HOST};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True"
EXPOSE 8080

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
