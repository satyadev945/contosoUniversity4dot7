# ContosoUniversity – Deployment Guide (Azure AKS)

## 1. Overview
ASP.NET Core 8 MVC app (EF6 + SQL Server, Application Insights). Container listens on **8080**, health endpoint **/health**.
The SQL Server database is external and supplied via `ConnectionStrings__SchoolContext`.

> **Base image note:** the requested runtime image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows / .NET Framework image and cannot run this `net8.0` application (nor be scheduled on Linux AKS nodes). The Dockerfile therefore uses `mcr.microsoft.com/dotnet/aspnet:8.0`.

## 2. Prerequisites
Docker, Azure CLI (`az login`), kubectl, an Azure subscription, an AKS cluster with Application Gateway Ingress Controller (AGIC), a registry (ACR or Docker Hub), and a reachable SQL Server.

## 3. Local development (Docker Compose)
```bash
cd ContosoUniversity
DB_CONNECTION_STRING="Server=host;Database=School;User Id=u;Password=p;TrustServerCertificate=True" docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```

## 4. Build and push
Run from the repository root:
```bash
./ContosoUniversity/scripts/build-push.sh      # Windows: ContosoUniversity\scripts\build-push.bat
```
Choose ACR or Docker Hub; the script sanitizes name/tag (lowercase, hyphenated, default `latest`) and prints the image URI. Manual equivalent:
`docker build -f ContosoUniversity/Dockerfile -t <registry>/contosouniversity:<tag> .`

## 5. AKS deployment
```bash
./ContosoUniversity/scripts/deploy-image.sh     # Windows: ContosoUniversity\scripts\deploy-image.bat
```
The script prompts for resource group, cluster, image URI, namespace and optional env values, substitutes `{{IMAGE_URI}}`, `{{NAMESPACE}}`, `{{CONNECTION_STRING}}`, `{{APPINSIGHTS_CONNECTION_STRING}}` in a temporary copy of the manifests, runs `az aks get-credentials`, applies namespace → deployment → service → ingress, waits for the rollout and prints the ingress address.

| Manifest | Purpose |
|---|---|
| namespace.yaml | Namespace `contosouniversity` |
| deployment.yaml | 2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health` |
| service.yaml | ClusterIP, 80 → 8080 |
| ingress.yaml | AGIC ingress, host `contosouniversity.example.com` (edit to your domain) |

If using ACR, attach it to the cluster: `az aks update -g <rg> -n <cluster> --attach-acr <acr>`.

## 6. Configuration
| Variable | Description |
|---|---|
| ASPNETCORE_ENVIRONMENT | `Production` |
| ASPNETCORE_URLS | `http://+:8080` |
| ConnectionStrings__SchoolContext | SQL Server connection string |
| APPLICATIONINSIGHTS_CONNECTION_STRING | Optional telemetry |

For production prefer Kubernetes Secrets / Azure Key Vault CSI driver over inline env values.

## 7. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 8. Troubleshooting
- Pod `ImagePullBackOff`: check image URI, ACR attach / pull secret.
- Pod not Ready: `kubectl logs`, `kubectl describe pod`; verify the connection string and SQL firewall rules.
- Service: `kubectl get endpoints -n contosouniversity`; `kubectl port-forward svc/contosouniversity-service 8080:80 -n contosouniversity`.
- Ingress has no address / 404: check AGIC pod logs and that the Host header matches the ingress host.

## 9. Security
Container runs as the non-root `app` user; no extra packages are installed; keep secrets out of manifests/VCS; enable HTTPS on the Application Gateway; restrict SQL access to the cluster network.

## 10. .NET notes
EF6 migrations/initializer run on first DB access; the connection string must be valid at runtime. Memory limit 1Gi is honored by the .NET GC in containers. Consider ReadyToRun publish and Serilog for structured logging.
