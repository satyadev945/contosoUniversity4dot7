# ContosoUniversity - Deployment Guide (Azure AKS)

## 1. Overview
ContosoUniversity is an ASP.NET Core MVC application (net8.0, Entity Framework Core + SQL Server). It listens on port **8080** and exposes a health endpoint at **/health**.

> **Base image note:** the requested explicit base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows / .NET Framework image and cannot run a net8.0 application (nor schedule on Linux AKS nodes). The Dockerfile therefore uses `mcr.microsoft.com/dotnet/sdk:8.0` (build) and `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime).

## 2. Prerequisites
- Docker Desktop / Docker Engine
- Azure CLI (`az`) and an Azure subscription (`az login`)
- kubectl
- An AKS cluster with the Application Gateway Ingress Controller (AGIC) add-on
- An ACR (or Docker Hub account), attached to AKS: `az aks update -g <rg> -n <cluster> --attach-acr <acr>`
- A reachable SQL Server instance (not part of these artifacts)

## 3. Layout
```
ContosoUniversity/
  Dockerfile, docker-compose.yml, .dockerignore
  kubernetes/  namespace.yaml deployment.yaml service.yaml ingress.yaml
  scripts/     build-push.sh|bat  deploy-image.sh|bat
  docs/DEPLOYMENT.md
.dockerignore   (repository root, used by the build context)
```
The Docker build context is the **repository root**: `docker build -f ContosoUniversity/Dockerfile .`

## 4. Local run with Docker Compose
```
cd ContosoUniversity
export DB_HOST=host.docker.internal DB_PASSWORD='<password>'
docker compose up --build
```
Browse http://localhost:8080 ; health: http://localhost:8080/health. Compose contains only the application; SQL Server must be provided externally.

## 5. Build and push
```
./ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat         # Windows
```
Choose ACR or Docker Hub, enter a tag (default `latest`). Names/tags are lowercased and sanitised. The script prints the final image URI.

## 6. Deploy to AKS
```
./ContosoUniversity/scripts/deploy-image.sh
```
The script prompts for resource group, cluster, image URI, DB settings and ingress host. It substitutes `{{IMAGE_URI}}`, `{{DB_*}}`, `{{INGRESS_HOST}}` in a temporary copy of the manifests, runs `az aks get-credentials`, creates the `contosouniversity-db` secret (DB_PASSWORD), applies namespace -> deployment -> service -> ingress, waits for the rollout and prints resources.

Manifests: namespace `contosouniversity`; Deployment (2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health:8080`, non-root uid 1654); Service ClusterIP 80->8080; Ingress with `kubernetes.io/ingress.class: azure/application-gateway`.

## 7. Configuration
| Variable | Purpose | Default |
|---|---|---|
| ASPNETCORE_ENVIRONMENT | environment | Production |
| ASPNETCORE_URLS | Kestrel binding | http://+:8080 |
| ConnectionStrings__SchoolContext | full connection string (overrides DB_*) | - |
| DB_HOST / DB_PORT / DB_NAME / DB_USER | SQL Server settings | localhost / 1433 / ContosoUniversity / sa |
| DB_PASSWORD | SQL password (Kubernetes secret) | empty |

The app runs `SchoolInitializer.Initialize` at startup, so the database must be reachable before pods become ready (the startup probe allows ~3 minutes).

## 8. Operations
- Status: `kubectl get pods,svc,ingress -n contosouniversity`
- Logs: `kubectl logs deployment/contosouniversity -n contosouniversity`
- Scale: `kubectl scale deployment/contosouniversity --replicas=4 -n contosouniversity`
- HPA: `kubectl autoscale deployment contosouniversity --cpu-percent=70 --min=2 --max=6 -n contosouniversity`
- Update image: `kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity`
- Rollback: `kubectl rollout undo deployment/contosouniversity -n contosouniversity`

## 9. Troubleshooting
- **ImagePullBackOff**: attach ACR to AKS or create an imagePullSecret; verify the image URI.
- **CrashLoopBackOff / not ready**: check logs; usually SQL Server unreachable or wrong credentials (DB_HOST/DB_PASSWORD).
- **Service has no endpoints**: `kubectl get endpoints -n contosouniversity`; check label `app=contosouniversity` and probes.
- **Ingress has no address / 502**: confirm AGIC add-on is enabled, `kubectl describe ingress`, and that `/health` returns 200.

## 10. Security
- Container runs as non-root (`USER $APP_UID`, uid 1654); no package managers or curl in the runtime image.
- DB password is kept in a Kubernetes Secret; consider Azure Key Vault CSI driver.
- Enable TLS on the Application Gateway / ingress, restrict SQL Server network access, and scan images (Defender for Containers).

## 11. .NET notes
- Workstation GC is configured (`DOTNET_gcServer=0`), which suits the 1Gi limit; `InvariantGlobalization=true` is set in the project.
- Consider Application Insights and Serilog for observability.
