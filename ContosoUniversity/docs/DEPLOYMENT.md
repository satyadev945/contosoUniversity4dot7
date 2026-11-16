# ContosoUniversity – Deployment Guide (AWS EKS)

## Application summary
- ASP.NET Core MVC on **.NET 8** (`net8.0`), Entity Framework 6 + Npgsql (PostgreSQL)
- Listens on **8080** (`ASPNETCORE_URLS=http://+:8080`); health endpoint **`/health`**
- Database connection supplied via `ConnectionStrings__SchoolContext`
- Linux container (`mcr.microsoft.com/dotnet/aspnet:8.0`), non-root user `app` (UID 1654)

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image and cannot run a `net8.0` application (and EKS Linux nodes cannot run it). The Dockerfile therefore uses `mcr.microsoft.com/dotnet/aspnet:8.0`.

## Prerequisites
Docker, AWS CLI v2 (configured), kubectl, eksctl (optional), an EKS cluster with AWS Load Balancer Controller installed, an ECR/Docker Hub account, a reachable PostgreSQL database. IAM permissions: `ecr:*` for push, `eks:DescribeCluster`, and cluster access via aws-auth / access entries.

## Local development
```bash
cd ContosoUniversity
export SCHOOL_CONNECTION_STRING="Host=host.docker.internal;Port=5432;Database=ContosoUniversity;Username=postgres;Password=secret"
docker compose up --build
# http://localhost:8080 , http://localhost:8080/health
```

## Build and push (from repository root)
```bash
./ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat          # Windows
```
The script prompts for the tag (sanitised, default `latest`), registry (ECR or Docker Hub), creates the ECR repository if missing, builds with `docker build -f ContosoUniversity/Dockerfile -t <image> .` and pushes.

## Deploy to EKS
```bash
cd ContosoUniversity
./scripts/deploy-image.sh      # or scripts\deploy-image.bat
```
The script prompts for region, cluster, image URI and the DB connection string; replaces `{{IMAGE_URI}}` and `{{CONNECTIONSTRINGS__SCHOOLCONTEXT}}` in a temporary copy of the manifests; runs `aws eks update-kubeconfig`; applies namespace → deployment → service → ingress; waits for rollout and prints the ALB hostname.

Manifests (`kubernetes/`):
- `namespace.yaml` – `contosouniversity`
- `deployment.yaml` – 2 replicas, requests 250m/512Mi, limits 500m/1Gi, HTTP probes on `/health`:8080
- `service.yaml` – ClusterIP 80 → 8080
- `ingress.yaml` – ALB (internet-facing, ip targets); change host `contosouniversity.example.com`

## Configuration
| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__SchoolContext` | PostgreSQL connection string (use a Kubernetes Secret in production) |

## Scaling and operations
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment/contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl rollout history deployment/contosouniversity -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## Troubleshooting
- **Pods CrashLoopBackOff**: `kubectl logs deploy/contosouniversity -n contosouniversity`; verify DB connection string and network/security-group access to PostgreSQL.
- **Pods not ready**: confirm `/health` returns 200; check probe timing.
- **ImagePullBackOff**: verify image URI and node role has ECR read permission (or add imagePullSecret for Docker Hub).
- **Ingress has no address**: ensure AWS Load Balancer Controller is installed and subnets are tagged; `kubectl describe ingress -n contosouniversity`.
- **Service unreachable**: `kubectl get endpoints -n contosouniversity`.

## Security
Non-root container, no extra packages in runtime image, store secrets in Kubernetes Secrets / AWS Secrets Manager, add HTTPS with an ACM certificate annotation on the Ingress, restrict DB access to cluster security groups.

## .NET notes
`InvariantGlobalization` is enabled (no ICU needed). The `/health` check only verifies the app is running, not the DB. EF6 migrations/initializer run on first DB access.
