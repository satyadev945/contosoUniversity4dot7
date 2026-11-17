# ContosoUniversity – Deployment Guide (Azure AKS)

## 1. Overview
ASP.NET Core MVC app (net8.0, Entity Framework 6.5.1, Application Insights) listening on **port 8080** with a health endpoint at **/health**.
The image is a Linux container: `mcr.microsoft.com/dotnet/sdk:8.0` (build) -> `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime), running as non-root.

> The requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image and cannot run a `net8.0` application,
> so the .NET 8 ASP.NET image is used. AKS Linux node pools are used.

## 2. Prerequisites
Docker, Azure CLI (`az login`), kubectl, an Azure subscription, an ACR (or Docker Hub account), an AKS cluster with the Application Gateway Ingress Controller (AGIC) add-on, and an external SQL Server reachable from the cluster.

## 3. Configuration
| Variable | Purpose |
|---|---|
| `ConnectionStrings__SchoolContext` | SQL Server connection string (read by `SchoolContext`) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Optional; enables telemetry |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |

For production, store the connection string in a Kubernetes Secret / Azure Key Vault rather than plain deployment values.

## 4. Local run (Docker Compose)
```
export SCHOOL_CONNECTION_STRING="Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```

## 5. Build and push
Run from the repository root:
```
./scripts/build-push.sh        # Windows: scripts\build-push.bat
```
Choose ACR or Docker Hub, enter a tag (default `latest`). Command used: `docker build -f ContosoUniversity/Dockerfile -t <registry>/contosouniversity:<tag> .`

## 6. Deploy to AKS
```
./scripts/deploy-image.sh      # Windows: scripts\deploy-image.bat
```
The script prompts for resource group, cluster, image URI and env values, substitutes placeholders (`{{IMAGE_URI}}`, `{{SCHOOL_CONNECTION_STRING}}`, `{{APPLICATIONINSIGHTS_CONNECTION_STRING}}`) in a temporary copy of the manifests, runs `az aks get-credentials`, applies namespace -> deployment -> service -> ingress, waits for rollout and prints resources.

Manifests (`kubernetes/`): `namespace.yaml` (contosouniversity), `deployment.yaml` (2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness probes on `/health`), `service.yaml` (ClusterIP 80 -> 8080), `ingress.yaml` (AGIC, host `contosouniversity.example.com` – change to your domain).

Attach ACR to AKS: `az aks update -g <rg> -n <cluster> --attach-acr <acr>`.

## 7. Operations
- Scale: `kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4`; HPA: `kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6`
- Update: re-run deploy script with a new tag (rolling update).
- Rollback: `kubectl rollout undo deployment/contosouniversity -n contosouniversity`

## 8. Troubleshooting
- `ImagePullBackOff`: ACR not attached to AKS or wrong URI/tag.
- `CrashLoopBackOff`: `kubectl logs deployment/contosouniversity -n contosouniversity`; check the connection string.
- Probes failing: verify `/health` returns 200 (`kubectl port-forward svc/contosouniversity-service 8080:80 -n contosouniversity`).
- Ingress has no address: check the AGIC add-on (`kubectl get pods -n kube-system | grep ingress`) and `kubectl describe ingress`.
- DB errors: allow the AKS outbound IP / VNet in the SQL Server firewall.

## 9. Security
Non-root container, no extra packages in the runtime image, secrets via Kubernetes Secrets/Key Vault, TLS on the Application Gateway, restrict SQL firewall, scan images with Defender for Containers.

## 10. .NET notes
Entity Framework 6 on .NET 8 is supported but legacy; `InvariantGlobalization` is enabled. Migrations/initializers need DB permissions at startup. Tune memory with `DOTNET_GCHeapHardLimitPercent` if needed.
