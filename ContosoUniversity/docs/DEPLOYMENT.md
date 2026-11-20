# ContosoUniversity - Deployment Guide (Azure AKS)

ASP.NET Core 8 MVC application (Entity Framework Core + SQL Server). Container listens on **8080**, health endpoint **/health**.

> Note: the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows / .NET Framework image and cannot run this `net8.0` application. The Dockerfile therefore uses `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux).

## Prerequisites
- Docker 24+ (Linux containers), Azure CLI, kubectl
- Azure subscription, AKS cluster with Application Gateway Ingress Controller (AGIC), ACR (or Docker Hub account)
- An externally hosted SQL Server reachable from the cluster

## Configuration
| Variable | Purpose |
|---|---|
| ConnectionStrings__SchoolContext | Full SQL Server connection string (takes precedence) |
| DB_HOST / DB_PORT / DB_NAME / DB_USER / DB_PASSWORD | Used to compose the connection string when the above is empty |
| DB_INITIALIZE | `true` runs `DbInitializer` (creates/seeds DB) on startup |
| ASPNETCORE_ENVIRONMENT / ASPNETCORE_URLS | `Production` / `http://+:8080` |

## Local run (Docker Compose)
```bash
cd ContosoUniversity
export DB_HOST=<sql-host> DB_USER=<user> DB_PASSWORD=<password> DB_INITIALIZE=true
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
Only the application is defined; SQL Server must be provided separately.

## Build and push
Run from the repository root:
```bash
ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
Choose ACR or Docker Hub; the script sanitises the image name/tag (default tag `latest`) and runs
`docker build -f ContosoUniversity/Dockerfile -t <registry>/contosouniversity:<tag> .`

## Deploy to AKS
```bash
ContosoUniversity/scripts/deploy-image.sh
```
The script prompts for resource group, cluster, image URI and DB settings, renders `{{PLACEHOLDER}}` values in a temp copy of `kubernetes/`, fetches credentials, creates the `contosouniversity-db` secret (DB password / connection string), then applies namespace -> deployment -> service -> ingress and waits for the rollout.

Manifests:
- `namespace.yaml` - `contosouniversity`
- `deployment.yaml` - 2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`, runs as UID 1654
- `service.yaml` - ClusterIP 80 -> 8080
- `ingress.yaml` - AGIC, host `{{INGRESS_HOST}}`

Ensure the AKS cluster can pull from ACR: `az aks update -g <rg> -n <cluster> --attach-acr <acr>`.

## Operations
```bash
kubectl get pods,svc,ingress -n contosouniversity
kubectl logs -l app=contosouniversity -n contosouniversity
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## Troubleshooting
- **ImagePullBackOff**: attach ACR to AKS or create an image pull secret.
- **CrashLoop / not ready**: `/health` only reports the process is up; check logs for SQL connectivity errors (firewall, credentials, `TrustServerCertificate`).
- **Ingress has no address**: verify AGIC add-on is enabled (`az aks show ... --query addonProfiles`).
- **502 from App Gateway**: confirm the health probe path `/health` returns 200.

## Security
- Runs as non-root (UID 1654), no privilege escalation, all capabilities dropped.
- DB credentials are stored in a Kubernetes Secret; consider Azure Key Vault CSI driver and Workload Identity.
- Terminate TLS at the Application Gateway; add HTTPS annotations/certificates for production.
- The app does not use HTTPS redirection inside the container (TLS terminates at the ingress).
