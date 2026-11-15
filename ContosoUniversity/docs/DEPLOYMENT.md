# ContosoUniversity – Deployment Guide (Docker & Azure AKS)

ASP.NET Core 8 MVC application (EF Core + SQL Server, Application Insights). The container listens on **port 8080** and exposes the health endpoint **`/health`**.

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows / .NET Framework image and cannot run this `net8.0` application (nor be scheduled on a Linux AKS node pool). The Dockerfile therefore uses `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime) and `mcr.microsoft.com/dotnet/sdk:8.0` (build).

## 1. Prerequisites
- Docker 24+ (Linux containers)
- Azure CLI (`az`) and `kubectl`
- An Azure subscription, an AKS cluster (Linux node pool) with the Application Gateway Ingress Controller (AGIC) add-on, and an ACR (or Docker Hub account)
- A reachable SQL Server instance (not part of these artifacts)

## 2. Layout
```
ContosoUniversity/
  Dockerfile              (build context = repository root)
  docker-compose.yml      (application only)
  .dockerignore
  kubernetes/             namespace.yaml, deployment.yaml (Secret + Deployment), service.yaml, ingress.yaml
  scripts/                build-push.sh/.bat, deploy-image.sh/.bat
  docs/DEPLOYMENT.md
```
All scripts are executed from the **repository root** (folder with `ContosoUniversity.sln`) so the Docker build context is `.`.

## 3. Configuration
| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__SchoolContext` | Full SQL Server connection string (takes precedence) |
| `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Used to compose the connection string when the above is not set |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Optional telemetry |

The app runs `DbInitializer.Initialize` at startup, so the database must be reachable before the pod becomes ready (a startup probe allows up to 5 minutes).

## 4. Local run with Docker Compose
```bash
cd ContosoUniversity
export DB_HOST=<sql-host> DB_USER=sa DB_PASSWORD='<password>'
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```

## 5. Build & push the image
```bash
./ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat         # Windows
```
Choose 1 (ACR) or 2 (Docker Hub). The image name is `contosouniversity`; the tag defaults to `latest` and is sanitised (lower-case, hyphenated). Equivalent manual command:
```bash
docker build -f ContosoUniversity/Dockerfile -t <registry>/contosouniversity:<tag> .
```

## 6. Deploy to AKS
```bash
./ContosoUniversity/scripts/deploy-image.sh      # or deploy-image.bat
```
The script prompts for resource group, cluster, image URI, ingress host and DB settings; substitutes `{{IMAGE_URI}}`, `{{INGRESS_HOST}}`, `{{DB_*}}`, `{{APPLICATIONINSIGHTS_CONNECTION_STRING}}` in a temporary copy of the manifests; runs `az aks get-credentials`; applies namespace → deployment → service → ingress; waits for the rollout and prints resources.

Manifests:
- `namespace.yaml` – namespace `contosouniversity`
- `deployment.yaml` – Secret (DB password / App Insights) + Deployment (2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`, non-root)
- `service.yaml` – ClusterIP `contosouniversity-service` 80 → 8080
- `ingress.yaml` – AGIC ingress (`kubernetes.io/ingress.class: azure/application-gateway`)

If the AKS cluster is not attached to your ACR: `az aks update -g <rg> -n <cluster> --attach-acr <acr>`.

## 7. Operations
```bash
kubectl get pods,svc,ingress -n contosouniversity
kubectl logs deployment/contosouniversity -n contosouniversity
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 8. Troubleshooting
- **ImagePullBackOff** – attach ACR to AKS or create an image pull secret; verify the image URI/tag.
- **CrashLoopBackOff / not ready** – check logs; usually SQL Server unreachable or wrong credentials (startup DB initialisation). Allow the AKS outbound IP in the SQL firewall.
- **Probe failures** – `kubectl describe pod`; `/health` is served on 8080 by `MapHealthChecks`.
- **Ingress has no address / 502** – confirm AGIC is enabled (`az aks show ... --query addonProfiles`), the service has endpoints (`kubectl get endpoints -n contosouniversity`), and the DNS name points to the gateway IP.
- **Globalization errors** – the image sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` (no ICU in the runtime image).

## 9. Security
- Container runs as non-root UID 1654; privilege escalation disabled.
- DB password and Application Insights string are stored in a Kubernetes Secret created at deploy time; for production use Azure Key Vault (CSI driver) or workload identity and don't keep passwords in shell history.
- Add TLS to the ingress (`appgw.ingress.kubernetes.io/ssl-redirect`, cert in Key Vault / cert-manager) – the app itself serves HTTP only behind the gateway.
- Prefer Azure SQL with private endpoints and `TrustServerCertificate=False` for production.

## 10. .NET notes
- Kestrel binds 8080 (non-privileged). Memory limit 1Gi: the .NET GC honours container limits automatically.
- The sample `appsettings.json` contains no secrets; override via environment variables (`Section__Key`).
