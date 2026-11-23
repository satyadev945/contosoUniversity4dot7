# ContosoUniversity – Deployment Guide (AWS EKS)

## Application summary
- ASP.NET Core 8 MVC app (`net8.0`), EF Core 8 SQL Server provider, `dotnet` CLI build.
- Listens on **8080** (`ASPNETCORE_URLS=http://+:8080`). Health endpoint: **`GET /health`**.
- Requires an external SQL Server: `ConnectionStrings__SchoolContext` (or `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`).
- Note: the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows .NET Framework image and cannot run a `net8.0` application, so `mcr.microsoft.com/dotnet/aspnet:8.0` is used.

## Prerequisites
Docker, AWS CLI v2 (configured credentials), kubectl, eksctl (optional), an EKS cluster with the AWS Load Balancer Controller installed, an ECR repo or Docker Hub account, IAM permissions for ECR (`ecr:*` push actions) and `eks:DescribeCluster`.

## Local run (Docker Compose)
```bash
cd ContosoUniversity
export CONNECTION_STRING="Server=host.docker.internal;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```

## Build and push
Run from the **repository root** (the Docker build context):
```bash
bash ContosoUniversity/scripts/build-push.sh        # Windows: ContosoUniversity\scripts\build-push.bat
```
The script prompts for tag and registry (ECR or Docker Hub), creates the ECR repository if missing, builds and pushes.

## Deploy to EKS
```bash
bash ContosoUniversity/scripts/deploy-image.sh      # Windows: ContosoUniversity\scripts\deploy-image.bat
```
Prompts: region, cluster name, image URI, SQL connection string. Manifests are rendered in a temp dir (`{{IMAGE_URI}}`, `{{CONNECTION_STRING_B64}}`), then applied in order: namespace → deployment (+Secret) → service → ingress.

Manifests (`kubernetes/`):
- `namespace.yaml` – namespace `contosouniversity`.
- `deployment.yaml` – Secret with the connection string; Deployment with 2 replicas, non-root (UID 1654), requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`.
- `service.yaml` – ClusterIP, 80 → 8080.
- `ingress.yaml` – ALB (`internet-facing`, `target-type: ip`, health check `/health`). Add a `host:` rule and TLS annotations (`alb.ingress.kubernetes.io/certificate-arn`) for production.

## Operations
```bash
kubectl get pods,svc,ingress -n contosouniversity
kubectl logs deployment/contosouniversity -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
kubectl scale deployment/contosouniversity --replicas=4 -n contosouniversity
kubectl autoscale deployment contosouniversity --cpu-percent=70 --min=2 --max=6 -n contosouniversity
```

## Troubleshooting
- **Pod CrashLoopBackOff**: check logs; missing connection string throws at startup. Verify SQL Server reachability from the cluster (security groups, VPC routing).
- **ImagePullBackOff**: node role needs `AmazonEC2ContainerRegistryReadOnly`; verify the image URI.
- **Readiness failing**: the app creates/seeds the DB on startup; the startupProbe allows ~150s.
- **Ingress has no address**: confirm the AWS Load Balancer Controller is installed and subnets are tagged (`kubernetes.io/role/elb`).
- **Target group unhealthy**: health check path must be `/health` on port 8080.

## Security notes
Non-root container, privilege escalation disabled, all capabilities dropped. For production use AWS Secrets Manager (External Secrets / CSI driver) instead of a plain Secret, enable HTTPS on the ALB, and restrict database access by security group.

## .NET notes
Set memory limits so the GC respects container limits (automatic in .NET 8). Logs go to stdout. `AppSettings__webpages__Enabled` overrides the `AppSettings:webpages:Enabled` setting.
