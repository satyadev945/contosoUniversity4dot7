# ContosoUniversity - Deployment Guide (Docker / AWS EKS)

## Application summary
- ASP.NET Core MVC on **.NET 8** (`net8.0`), Entity Framework 6 (SQL Server via `System.Data.SqlClient`).
- Listens on **port 8080** (`ASPNETCORE_URLS=http://+:8080`). Health endpoint: **`/health`**.
- Requires an external SQL Server; connection string via env var `ConnectionStrings__SchoolContext` (the app throws on startup of DB access if missing).
- Runtime image: `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux). The requested `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image and cannot run a net8.0 app, so it is not used.

## Prerequisites
Docker 24+, AWS CLI v2, kubectl, (optional) eksctl; IAM permissions for ECR (push/create repo) and EKS (`eks:DescribeCluster`); an EKS cluster with the **AWS Load Balancer Controller** installed; reachable SQL Server.

## Local run (Docker Compose)
```bash
cd ContosoUniversity
export SCHOOL_DB_CONNECTION="Server=host,1433;Database=School;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  health: http://localhost:8080/health
```

## Build and push (run from repository root)
```bash
./ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
Choose AWS ECR or Docker Hub; the ECR repository is created automatically. The script prints the final image URI.

## Deploy to EKS
```bash
cd ContosoUniversity
./scripts/deploy-image.sh     # or scripts\deploy-image.bat
```
The script updates kubeconfig, creates the namespace, creates secret `contosouniversity-db` (key `ConnectionStrings__SchoolContext`), substitutes `{{IMAGE_URI}}` in deployment.yaml on the fly, applies deployment/service/ingress, and waits for rollout.

Manifests (`kubernetes/`): `namespace.yaml` (contosouniversity), `deployment.yaml` (2 replicas, 250m/512Mi requests, 500m/1Gi limits, HTTP probes on `/health`), `service.yaml` (ClusterIP 80 -> 8080), `ingress.yaml` (ALB, internet-facing, target-type ip).

## Operations
- Scale: `kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4`; or create an HPA: `kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6`.
- Rolling update: re-run deploy script with a new tag. Rollback: `kubectl rollout undo deployment/contosouniversity -n contosouniversity`.

## Troubleshooting
- Pods `ImagePullBackOff`: check ECR permissions on node role and image URI.
- Pods `CrashLoop` / not ready: `kubectl logs -n contosouniversity deploy/contosouniversity`; verify the secret and SQL Server network access (security groups).
- Ingress has no address: confirm AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`) and subnets are tagged; `kubectl describe ingress -n contosouniversity`.
- 502/503 from ALB: target health check path `/health` must return 200.

## Security
Container runs as the non-root `app` user; connection string is kept in a Kubernetes Secret (consider AWS Secrets Manager + External Secrets); add HTTPS via ACM certificate annotations (`alb.ingress.kubernetes.io/certificate-arn`, listen-ports 443) before production; restrict SQL Server access to the cluster.

## .NET notes
`InvariantGlobalization` is enabled. Server GC respects container limits automatically in .NET 8. EF6 on .NET 8 requires the `System.Data.SqlClient` provider and the EF6 migrations are not auto-applied.
