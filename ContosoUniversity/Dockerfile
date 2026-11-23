# Build context: the ContosoUniversity project directory
#   docker build -t contosouniversity .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore ContosoUniversity.csproj
COPY . .
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-restore \
    -p:DebugType=None -p:DebugSymbols=false -p:UseAppHost=false \
    && find /app/publish -name '*.pdb' -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Supply the database connection string at runtime:
#   -e ConnectionStrings__SchoolContext="Server=${DB_HOST};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};"
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
