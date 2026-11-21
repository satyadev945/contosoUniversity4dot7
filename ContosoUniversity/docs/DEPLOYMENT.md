# ContosoUniversity - Deployment Guide (GCP GKE)

## 1. Overview
ContosoUniversity is an ASP.NET Core 8 MVC application (Entity Framework 6 on `System.Data.SqlClient`) listening on **port 8080** and exposing a health endpoint at **`/health`**. It requires an **external SQL Server**; no database is containerized here.

> **Base image note:** The requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image and cannot run a `net8.0` app. The Dockerfile uses `mcr.microsoft.com/dotnet/sdk:8.0` (build) and `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime, Linux).

## 2. Prerequisites
- Docker 24+, Google Cloud SDK (`gcloud`), `kubectl` (with `gke-gcloud-auth-plugin`)
- A GCP project with billing, Artifact Registry and GKE APIs enabled
- A reachable SQL Server and its connection string

## 3. Local run with Docker Compose
From the `ContosoUniversity` directory:
```bash
export SCHOOL_DB_CONNECTION="Server=host,1433;Database=School;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
```
Browse to http://localhost:8080 ; health: http://localhost:8080/health

## 4. Build and push image
Run from the repository root (`ApSdbzMono`):
```bash
./ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
Choose Google Artifact Registry (create a repo first: `gcloud artifacts repositories create REPO --repository-format=docker --location=REGION`) or Docker Hub. Tags are lowercased and sanitized; empty tag becomes `latest`. Build context is always `.` with `-f ContosoUniversity/Dockerfile`.

## 5. GKE cluster setup
```bash
gcloud container clusters create-auto my-cluster --region us-central1 --project PROJECT
gcloud container clusters get-credentials my-cluster --region us-central1 --project PROJECT
```
(For zonal clusters pass `--zone`; the deploy script uses `--zone`, which also accepts a zone name. For regional clusters edit the script to use `--region`.)

## 6. Deploy
```bash
./ContosoUniversity/scripts/deploy-image.sh
```
The script prompts for project, zone, cluster, image URI and `SCHOOL_DB_CONNECTION`, then:
1. Copies the manifests to a temp dir and substitutes `{{IMAGE_URI}}` (sed with `|` delimiter).
2. Runs `gcloud container clusters get-credentials` and `kubectl cluster-info`.
3. Applies `namespace.yaml`, creates the `contosouniversity-secrets` Secret, applies `deployment.yaml`, `service.yaml`, `ingress.yaml`.
4. Waits for `kubectl rollout status` and prints resources and the ingress IP.

### Manifests (`kubernetes/`)
| File | Purpose |
|------|---------|
| namespace.yaml | `contosouniversity` namespace |
| deployment.yaml | 2 replicas, non-root (uid 1654), requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health:8080` |
| service.yaml | ClusterIP 80 -> 8080, with NEG annotation for container-native load balancing |
| ingress.yaml | GKE (`gce`) HTTP load balancer routing `/` to the service |

## 7. Configuration
| Variable | Description |
|----------|-------------|
| `SCHOOL_DB_CONNECTION` | SQL Server connection string (Kubernetes Secret). Alternative: `ConnectionStrings__SchoolContext` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |

For production use Secret Manager / Workload Identity (e.g. External Secrets or the Secrets Store CSI driver) rather than manually created secrets. The database must be reachable from the cluster (VPC peering, Cloud SQL proxy, etc.).

## 8. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=NEW_IMAGE -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```
Rolling updates use maxUnavailable=0 / maxSurge=1.

## 9. Troubleshooting
- **Pods CrashLoopBackOff:** `kubectl logs deploy/contosouniversity -n contosouniversity` - most commonly a missing/invalid `SCHOOL_DB_CONNECTION`.
- **ImagePullBackOff:** verify image URI; grant the node service account `roles/artifactregistry.reader`.
- **Pods not Ready:** `kubectl describe pod ...`; check `/health` via `kubectl port-forward svc/contosouniversity-service 8080:80 -n contosouniversity`.
- **Ingress 502 / no IP:** provisioning takes 5-10 minutes; `kubectl describe ingress contosouniversity-ingress -n contosouniversity`. Ensure the GCLB health check path is `/health` (add a BackendConfig if needed).
- **DB timeouts:** check firewall rules and that the SQL Server accepts connections from the cluster's egress IP.

## 10. Security
- Container runs as non-root (uid 1654), no privilege escalation, all capabilities dropped.
- Terminate TLS at the load balancer (Google-managed certificate + HTTPS redirect via FrontendConfig).
- Keep secrets out of images and manifests; scan images with Artifact Analysis.

## 11. .NET notes
- `InvariantGlobalization` is enabled (no ICU needed); set `TZ` as needed.
- EF6 initializers/migrations run against the external database - ensure the account has the required rights.
- Consider ASP.NET Core DataProtection key persistence if multiple replicas use antiforgery tokens/cookies (shared key ring in a PVC or GCS).
