# ContosoUniversity – Deployment Guide (Azure AKS)

## 1. Application summary
- ASP.NET Core MVC on .NET 8 (`net8.0`), EF Core SqlServer, Application Insights.
- Listens on port **8080** (`ASPNETCORE_URLS=http://+:8080`); health endpoint **`/health`**.
- Configuration (env vars): `ConnectionStrings__SchoolContext` **or** `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`; optional `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- Base image: the requested `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows .NET Framework image and cannot host a net8.0 app, so `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux) is used.

## 2. Prerequisites
Docker 24+, Azure CLI, kubectl, Azure subscription, an ACR (or Docker Hub account), an AKS cluster with the Application Gateway Ingress Controller (AGIC) addon, and a reachable SQL Server.

## 3. Local run with Docker Compose
```
cd ContosoUniversity
DB_HOST=<sql-host> DB_PASSWORD=<pwd> docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
The compose file defines only the application; SQL Server is external.

## 4. Build and push
Run from the repository root:
```
./ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
Choose 1 (ACR) or 2 (Docker Hub); the tag is sanitized (lowercase, default `latest`). The Docker build context is the repository root with `-f ContosoUniversity/Dockerfile`.

## 5. AKS setup
```
az login
az aks get-credentials --resource-group <rg> --name <cluster>
kubectl cluster-info
az aks update -g <rg> -n <cluster> --attach-acr <acr-name>   # allow pulling from ACR
```

## 6. Deploy
```
./ContosoUniversity/scripts/deploy-image.sh     # or deploy-image.bat
```
The script prompts for resource group, cluster, image URI, namespace, ingress host and DB settings, substitutes placeholders (`{{IMAGE_URI}}`, `{{NAMESPACE}}`, `{{INGRESS_HOST}}`, `{{DB_HOST}}`, `{{DB_NAME}}`, `{{DB_USER}}`) in a temporary copy of the manifests, creates the `contosouniversity-secrets` Secret (DB_PASSWORD, App Insights), then applies namespace → deployment → service → ingress and waits for the rollout.

Manifests (`kubernetes/`): `namespace.yaml`, `deployment.yaml` (2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness on `/health`, non-root uid 1654), `service.yaml` (ClusterIP 80 → 8080), `ingress.yaml` (AGIC).

## 7. Operations
- Scale: `kubectl scale deployment/contosouniversity -n <ns> --replicas=4` or create an HPA (`kubectl autoscale deployment contosouniversity -n <ns> --cpu-percent=70 --min=2 --max=6`).
- Update: re-run deploy script with the new image tag (rolling update).
- Rollback: `kubectl rollout undo deployment/contosouniversity -n <ns>`.

## 8. Troubleshooting
- `ImagePullBackOff`: attach ACR to AKS or check image URI/credentials.
- `CrashLoopBackOff` / not ready: `kubectl logs deployment/contosouniversity -n <ns>`; the app runs `DbInitializer` at startup, so verify the SQL Server host, credentials and network/firewall access.
- Ingress has no address: confirm AGIC addon is enabled, `kubectl describe ingress -n <ns>`.
- 502 from App Gateway: check backend health probe path `/health`.

## 9. Security
Non-root container, privilege escalation disabled, all capabilities dropped; secrets are stored in a Kubernetes Secret (consider Azure Key Vault CSI driver); terminate TLS at the Application Gateway; do not use `TrustServerCertificate=True` with untrusted networks in production.

## 10. .NET notes
Set `DOTNET_gcServer=0` (workstation GC) for small pods; memory limits are honored by the runtime GC. `InvariantGlobalization` is enabled in the project. Consider Serilog and Application Insights for observability, and `/health/ready` with a DB check for better readiness semantics.
