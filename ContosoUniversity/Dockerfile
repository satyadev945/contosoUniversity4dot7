FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false \
    && find /app/publish -type f \( -name "*.pdb" \) -delete

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
