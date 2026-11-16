# Build context: the ContosoUniversity project directory
#   docker build -t contosouniversity .
# The project has been migrated to ASP.NET Core (net8.0), so the Windows
# aspnet:4.8 / IIS image is not applicable; the Linux ASP.NET Core runtime is used.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj ./
RUN dotnet restore ContosoUniversity.csproj
COPY . .
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false
RUN find /app/publish -name '*.pdb' -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
# Supply at runtime: ConnectionStrings__SchoolContext or DB_HOST/DB_NAME/DB_USER/DB_PASSWORD,
# and optionally APPLICATIONINSIGHTS_CONNECTION_STRING
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
