# Build context: the ContosoUniversity project directory
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore ContosoUniversity.csproj
COPY . .
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
# Supply at runtime, e.g. -e ConnectionStrings__SchoolContext="Server=${DB_HOST},1433;Database=...;User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True"
EXPOSE 8080

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
