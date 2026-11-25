# ContosoUniversity – Docker & Azure AKS Deployment Guide

ASP.NET Core 8.0 MVC application (EF Core + SQL Server, Application Insights). Listens on **port 8080**; health endpoint **`/health`**.

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and cannot host a `net8.0` application. The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux). If you must override it, the app would first need to be retargeted to .NET Framework.

## 1. Prerequisites
- Docker 24+ (Linux containers), Azure CLI 2.50+, kubectl, Azure subscription
- An AKS cluster with the Application Gateway Ingress Controller (AGIC) add-on, attached to your ACR (`az aks update -g <rg> -n <aks> --attach-acr <acr>`)
- A reachable SQL Server / Azure SQL database (not part of these artifacts)

## 2. Layout
```
ContosoUniversity/
  Dockerfile              (build context = repository root)
  docker-compose.yml      (application service only)
  kubernetes/             namespace, deployment (+Secret), service, ingress
  scripts/                build-push.sh/.bat, deploy-image.sh/.bat
  docs/DEPLOYMENT.md
```

## 3. Local run with Docker Compose
```bash
cd ContosoUniversity
export SCHOOL_CONNECTION_STRING="Server=host.docker.internal,1433;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true"
docker compose up --build
# http://localhost:8080  |  http://localhost:8080/health
```
The app throws on startup if `ConnectionStrings__SchoolContext` is empty, and on first start it creates and seeds the database (`EnsureCreated`, with retries).

## 4. Build and push
From anywhere (scripts resolve the repo root):
```bash
./ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
Choose 1 (ACR) or 2 (Docker Hub); tag is sanitised (lowercase, hyphenated, default `latest`). Equivalent manual command from the repo root:
`docker build -f ContosoUniversity/Dockerfile -t <registry>/contosouniversity:<tag> .`

## 5. Deploy to AKS
```bash
./ContosoUniversity/scripts/deploy-image.sh     # or deploy-image.bat
```
Prompts: resource group, cluster, image URI, namespace, ingress host, SQL connection string (required), Application Insights connection string (optional). The script substitutes `{{IMAGE_URI}}`, `{{NAMESPACE}}`, `{{INGRESS_HOST}}`, `{{CONNECTION_STRING}}`, `{{APPINSIGHTS_CONNECTION_STRING}}` in a temporary copy of the manifests, runs `az aks get-credentials`, applies namespace → deployment → service → ingress, and waits for rollout.

| Manifest | Purpose |
|---|---|
| namespace.yaml | Namespace `contosouniversity` |
| deployment.yaml | Secret + Deployment (2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness probes on `/health`, non-root UID 1654) |
| service.yaml | ClusterIP 80 → 8080 |
| ingress.yaml | AGIC ingress, host `{{INGRESS_HOST}}` |

## 6. Operations
```bash
kubectl get pods,svc,ingress -n contosouniversity
kubectl logs deploy/contosouniversity -n contosouniversity
kubectl scale deploy/contosouniversity --replicas=4 -n contosouniversity
kubectl autoscale deploy/contosouniversity --cpu-percent=70 --min=2 --max=6 -n contosouniversity
kubectl set image deploy/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deploy/contosouniversity -n contosouniversity
```

## 7. Troubleshooting
- **ImagePullBackOff**: attach ACR to AKS or create an imagePullSecret; verify the image URI/tag.
- **CrashLoopBackOff**: check logs; most often a missing/invalid `ConnectionStrings__SchoolContext` or SQL firewall blocking the AKS egress IP.
- **Pods not Ready**: `/health` must return 200; the startup probe allows ~150 s for the DB retry loop.
- **Ingress has no address / 502**: confirm AGIC is enabled (`kubectl get pods -n kube-system | grep ingress-appgw`) and the service has endpoints.
- **Rollout fails**: `kubectl describe pod`, then `kubectl rollout undo`.

## 8. Configuration
| Variable | Purpose |
|---|---|
| ASPNETCORE_ENVIRONMENT | `Production` |
| ASPNETCORE_URLS | `http://+:8080` |
| ConnectionStrings__SchoolContext | SQL Server connection (Secret) |
| APPLICATIONINSIGHTS_CONNECTION_STRING | Telemetry (optional) |

## 9. Security
- Container runs as non-root (UID 1654), no privilege escalation, no extra packages installed.
- Connection strings live in a Kubernetes Secret; prefer Azure Key Vault CSI driver and Workload Identity / managed identity for Azure SQL in production.
- Terminate TLS at Application Gateway (add `appgw.ingress.kubernetes.io/ssl-redirect: "true"` and a certificate).
- The deploy script renders secrets only into a temp directory that is deleted on exit.

## 10. .NET notes
- Memory limit 1Gi: .NET GC honours cgroup limits automatically; tune with `DOTNET_GCHeapHardLimitPercent` if needed.
- `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` avoids ICU dependencies; remove if culture-specific formatting is required.
- Multiple replicas call `EnsureCreated` at start; for production use EF Core migrations run as a separate job.
