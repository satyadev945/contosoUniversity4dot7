# ContosoUniversity - Deployment Guide (Docker and AWS EKS)

## 1. Application Overview
- ASP.NET Core MVC on .NET 8 (`net8.0`), Entity Framework 6 against SQL Server, Application Insights.
- Kestrel listens on **8080** (`ASPNETCORE_URLS=http://+:8080`, non-root friendly).
- Health endpoint: `GET /health` (ASP.NET Core health checks).
- Image: multi-stage build, `mcr.microsoft.com/dotnet/sdk:8.0` -> `mcr.microsoft.com/dotnet/aspnet:8.0`, runs as non-root user.

> Base image note: the requested explicit base image `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and cannot run a `net8.0` ASP.NET Core application, so the official `aspnet:8.0` Linux image is used.

## 2. Prerequisites
Docker 24+, AWS CLI v2, kubectl, eksctl (optional), an existing EKS cluster with the AWS Load Balancer Controller installed, an ECR repository or Docker Hub account, and a reachable SQL Server (not part of this deployment). IAM permissions: `ecr:*` for push, `eks:DescribeCluster`, and cluster RBAC rights.

## 3. Local Run (Docker Compose)
```bash
cd ContosoUniversity
export SCHOOL_DB_CONNECTION="Server=host;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
Build context is the repository root; the compose file handles that (`context: ..`).

## 4. Build and Push
Run from the repository root (`ApmOnonGd`):
```bash
./ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat         # Windows
```
The script prompts for tag (default `latest`, sanitised to lowercase), registry (1 = ECR, 2 = Docker Hub) and credentials. For ECR it creates the repository if missing. Equivalent manual command: `docker build -f ContosoUniversity/Dockerfile -t <registry>/<repo>:<tag> .`

## 5. EKS Setup
```bash
eksctl create cluster --name contoso --region us-east-1 --nodes 2     # if you need a cluster
aws eks update-kubeconfig --region us-east-1 --name contoso
kubectl cluster-info
```
Install the AWS Load Balancer Controller so the `alb` ingress class works.

## 6. Deploy to EKS
```bash
./ContosoUniversity/scripts/deploy-image.sh      # or deploy-image.bat
```
It prompts for region, cluster, image URI, ingress host, `SCHOOL_DB_CONNECTION` and `APPLICATIONINSIGHTS_CONNECTION_STRING`, substitutes `{{PLACEHOLDERS}}` in a temporary copy of the manifests, applies them in order and waits for the rollout.

Manifests (`kubernetes/`):
- `namespace.yaml` - namespace `contosouniversity`
- `deployment.yaml` - 2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`, port 8080
- `service.yaml` - ClusterIP, 80 -> 8080
- `ingress.yaml` - ALB, internet-facing, target-type ip, health check `/health`

For production, store the connection string in a Kubernetes Secret (or AWS Secrets Manager via the CSI driver) and reference it with `secretKeyRef` instead of a plain env value.

## 7. Configuration
| Variable | Purpose |
|---|---|
| ASPNETCORE_ENVIRONMENT | `Production` |
| ASPNETCORE_URLS | `http://+:8080` |
| SCHOOL_DB_CONNECTION (or ConnectionStrings__SchoolContext) | SQL Server connection (LocalDB is not available in Linux containers) |
| APPLICATIONINSIGHTS_CONNECTION_STRING | Telemetry (optional) |
| TZ | Time zone, default UTC |

## 8. Scaling and Management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 9. Troubleshooting
- Pods `CrashLoopBackOff`/not ready: `kubectl logs deploy/contosouniversity -n contosouniversity`; usually a bad or missing `SCHOOL_DB_CONNECTION`, or DB not reachable from the VPC (security groups).
- `ImagePullBackOff`: check image URI and node role ECR read permission.
- Ingress has no address: confirm the AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`) and subnets are tagged for ELB.
- ALB targets unhealthy: verify `/health` returns 200 (`kubectl port-forward svc/contosouniversity-service 8080:80 -n contosouniversity`).
- `kubectl describe pod/ingress -n contosouniversity` shows events.

## 10. Security
Non-root runtime user, no package managers or tools added to the image, secrets via Kubernetes Secrets, restrict DB access with security groups, add HTTPS (ACM certificate + `alb.ingress.kubernetes.io/certificate-arn`, listen-ports 443) before production use.

## 11. .NET Notes
- Set `DOTNET_gcServer=0` (default) and keep memory limits at least 512Mi; .NET honours container limits.
- `InvariantGlobalization` is enabled, so no ICU libraries are required.
- Entity Framework 6 on .NET 8 is supported via the `EntityFramework` 6.5.1 package; migrations run according to the app's DbConfiguration/initializer.
