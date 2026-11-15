# BASE_REGISTRY defaults to Microsoft Container Registry. In AWS, set it to the ECR pull-through cache
# prefix (e.g. <account>.dkr.ecr.<region>.amazonaws.com/mcr) so base images are served from ECR.
ARG BASE_REGISTRY=mcr.microsoft.com

FROM ${BASE_REGISTRY}/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContosoUniversity.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore /p:DebugType=none /p:DebugSymbols=false \
    && find /app/publish -type f \( -name "*.pdb" -o -name "*.tmp" \) -delete

FROM ${BASE_REGISTRY}/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
