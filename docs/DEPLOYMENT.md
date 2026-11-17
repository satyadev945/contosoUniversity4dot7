# ContosoUniversity - GCP GKE Deployment Guide

## 1. Overview
ContosoUniversity is an ASP.NET Core 8.0 MVC application (Entity Framework Core + SQL Server) listening on port **8080** inside the container, with a health endpoint at **/health**.

Note: the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image and is incompatible with this net8.0 app and Linux GKE nodes, so `mcr.microsoft.com/dotnet/aspnet:8.0` is used.

## 2. Prerequisites
- Docker 20.10+
- gcloud CLI (authenticated: `gcloud auth login`)
- kubectl (`gcloud components install kubectl gke-gcloud-auth-plugin`)
- A GCP project with Artifact Registry and GKE APIs enabled
- A reachable SQL Server instance (not part of these artifacts)

## 3. Local development (Docker Compose)
```bash
export SCHOOL_CONNECTION_STRING="Server=host.docker.internal;Database=ContosoUniversity;User Id=sa;Password=<pwd>;TrustServerCertificate=True"
docker compose up --build
```
Browse to http://localhost:8080 and http://localhost:8080/health. The app creates/seeds the database on startup (`SchoolInitializer`).

## 4. Build and push
Run from the repository root:
```bash
./scripts/build-push.sh      # Linux/macOS
scripts\build-push.bat       # Windows
```
Choose Google Artifact Registry (create repo: `gcloud artifacts repositories create <repo> --repository-format=docker --location=<region>`) or Docker Hub. Tags are lowercased/sanitized; default is `latest`.

## 5. GKE cluster setup
```bash
gcloud container clusters create contoso-cluster --zone us-central1-a --num-nodes 2
gcloud container clusters get-credentials contoso-cluster --zone us-central1-a --project <project>
```

## 6. Deploy
```bash
./scripts/deploy-image.sh     # or scripts\deploy-image.bat
```
The script prompts for project, zone, cluster, image URI and the SQL connection string, substitutes `{{IMAGE_URI}}`, `{{NAMESPACE}}`, `{{SCHOOL_CONNECTION_STRING}}` into a temporary copy of the manifests, and applies namespace -> deployment -> service -> ingress.

Manifests (`kubernetes/`):
- `namespace.yaml` - namespace `contosouniversity`
- `deployment.yaml` - 2 replicas, non-root (UID 1654), requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`
- `service.yaml` - ClusterIP 80 -> 8080 (NEG enabled)
- `ingress.yaml` - GCE ingress; change host `contosouniversity.example.com`

## 7. Configuration
| Variable | Purpose |
|---|---|
| ASPNETCORE_ENVIRONMENT | Production |
| ASPNETCORE_URLS | http://+:8080 |
| ConnectionStrings__SchoolContext | SQL Server connection string |

For production, store the connection string in a Kubernetes Secret (or Secret Manager CSI driver) rather than a plain env value.

## 8. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 9. Troubleshooting
- Pods CrashLoopBackOff: `kubectl logs -n contosouniversity deploy/contosouniversity`; usually the SQL connection string is wrong/unreachable (startup runs DB initialization).
- ImagePullBackOff: grant the node service account `roles/artifactregistry.reader`.
- Service issues: `kubectl get endpoints -n contosouniversity`.
- Ingress 502/unhealthy: wait several minutes for the GCE LB; verify `/health` returns 200; `kubectl describe ingress -n contosouniversity`.
- Cloud SQL / SQL Server must allow traffic from the GKE nodes/pod range.

## 10. Security
- Container runs as non-root; no extra packages installed.
- Use Secrets for credentials, enable HTTPS on the ingress (ManagedCertificate), restrict DB network access, scan images in Artifact Registry.
- Use Workload Identity instead of node-level credentials.

## 11. .NET notes
- Invariant globalization enabled; timezone UTC.
- Set `DOTNET_gcServer=0` and size memory limits (1Gi) to match GC heap behavior; consider `DOTNET_GCHeapHardLimitPercent`.
- Consider Serilog/Application Insights for observability.
