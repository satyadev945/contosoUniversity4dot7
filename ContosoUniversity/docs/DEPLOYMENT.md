# ContosoUniversity - Deployment Guide (Azure AKS)

ASP.NET Core MVC (.NET 8) application using Entity Framework 6.4 against an **external SQL Server** database.

## 1. Prerequisites
- Docker 24+ (with Compose v2), Azure CLI, kubectl
- An Azure subscription, an AKS cluster with the Application Gateway Ingress Controller (AGIC) add-on, and a registry (ACR or Docker Hub)
- A reachable SQL Server / Azure SQL database (not part of these artifacts)

## 2. Application facts
| Item | Value |
|------|-------|
| Runtime | .NET 8 (`mcr.microsoft.com/dotnet/aspnet:8.0`) |
| Port | 8080 (`ASPNETCORE_URLS=http://+:8080`, non-root friendly) |
| Health endpoint | `/health` |
| DB setting | `ConnectionStrings__SchoolContext` |
| Telemetry | `APPLICATIONINSIGHTS_CONNECTION_STRING` (optional) |

> Note: the requested base image `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and cannot run this `net8.0` application. The Linux ASP.NET Core 8 runtime image is used instead.

## 3. Local run with Docker Compose
```bash
export SCHOOL_CONNECTION_STRING="Server=host;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True"
cd ContosoUniversity
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
Compose builds with the repository root as context (`docker build -f ContosoUniversity/Dockerfile .`).

## 4. Build and push
```bash
./ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
The script prompts for tag (default `latest`, sanitized), registry type (1. ACR, 2. Docker Hub) and credentials, then builds and pushes `<registry>/contosouniversity:<tag>`.

## 5. Deploy to AKS
```bash
./ContosoUniversity/scripts/deploy-image.sh
```
The script prompts for resource group, cluster, image URI, ingress host and optional app settings, then:
1. Renders `{{IMAGE_URI}}`/`{{INGRESS_HOST}}` into a temp copy of the manifests (sed with `|` delimiter)
2. `az aks get-credentials` and `kubectl cluster-info`
3. Applies `namespace.yaml`, creates the `contoso-university-secrets` Secret (connection strings), then applies `deployment.yaml`, `service.yaml`, `ingress.yaml`
4. Waits for `kubectl rollout status` and prints pods/services/ingress and the URL

Manifests: `namespace` (contoso-university), `deployment` (2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness probes on `/health`, non-root UID 1654), `service` (ClusterIP 80 -> 8080), `ingress` (AGIC, `kubernetes.io/ingress.class: azure/application-gateway`).

## 6. Operations
```bash
kubectl get pods -n contoso-university
kubectl logs deploy/contoso-university -n contoso-university
kubectl scale deploy/contoso-university --replicas=4 -n contoso-university
kubectl autoscale deploy/contoso-university --cpu-percent=70 --min=2 --max=6 -n contoso-university
kubectl set image deploy/contoso-university contoso-university=<new-image> -n contoso-university
kubectl rollout undo deploy/contoso-university -n contoso-university
```

## 7. Troubleshooting
- **ImagePullBackOff**: attach ACR to AKS: `az aks update -g <rg> -n <cluster> --attach-acr <acr>`; verify image URI/tag.
- **CrashLoopBackOff / not ready**: `kubectl logs` and `kubectl describe pod`; verify the connection string secret and that the SQL firewall allows AKS egress.
- **Ingress has no address**: confirm AGIC add-on is enabled (`az aks show ... --query addonProfiles`), then `kubectl describe ingress -n contoso-university`.
- **502 from Application Gateway**: check `/health` returns 200 from the pod and the service selector matches pod labels.

## 8. Security
- Container runs as non-root (`app`, UID 1654), privilege escalation disabled, all capabilities dropped.
- Connection strings are stored in a Kubernetes Secret, never in images or manifests. Consider Azure Key Vault CSI driver and Workload Identity.
- Enable TLS on the ingress (cert in Key Vault / `appgw.ingress.kubernetes.io/ssl-redirect`).
- Do not commit credentials; rotate registry tokens.

## 9. .NET notes
- `InvariantGlobalization=true`; timezone `UTC`.
- Workstation GC (`DOTNET_gcServer=0`) suits the 1Gi memory limit.
- EF6 schema is created by the app's initializer/migrations against the configured database - ensure the DB user has the needed rights.
