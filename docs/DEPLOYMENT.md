# ContosoUniversity – Deployment Guide (Docker & AWS EKS)

## 1. Application summary
- ASP.NET Core MVC on **.NET 8** (`ContosoUniversity/ContosoUniversity.csproj`), Entity Framework 6 on SQL Server.
- Kestrel listens on **8080** (`ASPNETCORE_URLS=http://+:8080`); health endpoint **`/health`**.
- Required external configuration: `ConnectionStrings__SchoolContext` (SQL Server connection string). The SQL Server instance is NOT part of these artifacts.

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and cannot run a `net8.0` ASP.NET Core app (nor be scheduled on Linux EKS nodes). The Dockerfile therefore uses `mcr.microsoft.com/dotnet/sdk:8.0` (build) and `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime).

## 2. Prerequisites
Docker 24+, AWS CLI v2, kubectl, (optional) eksctl, an EKS cluster with Linux nodes and the AWS Load Balancer Controller installed, IAM permissions for ECR (`ecr:*` on the repo) and `eks:DescribeCluster`.

## 3. Local run with Docker Compose
```bash
export SCHOOL_CONNECTION_STRING="Server=host.docker.internal;Database=School;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  |  http://localhost:8080/health
```

## 4. Build and push (run from the repository root)
```bash
./scripts/build-push.sh      # Linux/macOS
scripts\build-push.bat       # Windows
```
Choose AWS ECR or Docker Hub; the script sanitises the tag (lowercase, defaults to `latest`), creates the ECR repository if missing, builds with `docker build -f ContosoUniversity/Dockerfile .` and pushes.

## 5. Deploy to EKS
```bash
./scripts/deploy-image.sh    # or scripts\deploy-image.bat
```
The script prompts for region, cluster, image URI and the connection string, then:
1. `aws eks update-kubeconfig` and `kubectl cluster-info`
2. applies `kubernetes/namespace.yaml`
3. creates secret `contoso-university-secrets` (key `ConnectionStrings__SchoolContext`)
4. applies deployment (with `{{IMAGE_URI}}` substituted using `sed` with `|` delimiter), service, ingress
5. waits for `kubectl rollout status` and prints the ALB hostname.

Manifests: Deployment (2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`, non-root UID 1654), Service (ClusterIP 80→8080), Ingress (ALB, internet-facing, IP target type, health check `/health`).

## 6. Operations
- Scale: `kubectl scale deployment/contoso-university -n contoso-university --replicas=4`; HPA: `kubectl autoscale deployment contoso-university -n contoso-university --cpu-percent=70 --min=2 --max=6`
- Rolling update: re-run deploy with a new image tag. Rollback: `kubectl rollout undo deployment/contoso-university -n contoso-university`
- Logs: `kubectl logs deployment/contoso-university -n contoso-university`

## 7. Troubleshooting
| Symptom | Check |
|---|---|
| `ImagePullBackOff` | Node role has ECR read permissions; image URI/tag correct |
| `CrashLoopBackOff` | `kubectl logs`; usually missing/invalid `ConnectionStrings__SchoolContext` (app throws on first DB use) |
| Pods not Ready | `/health` probe; `kubectl describe pod` |
| Ingress has no address | AWS Load Balancer Controller installed; subnets tagged `kubernetes.io/role/elb=1`; `kubectl describe ingress` |
| 502/503 from ALB | Target group health check path `/health`, security groups allow ALB→pod port 8080 |

## 8. Security
- Runs as non-root (UID 1654); connection string stored in a Kubernetes Secret (consider AWS Secrets Manager + External Secrets Operator).
- Add TLS: ACM certificate annotation `alb.ingress.kubernetes.io/certificate-arn` and listen on 443.
- Restrict database access to cluster security groups; use a least-privilege SQL login.

## 9. .NET notes
- `InvariantGlobalization=true`; no ICU packages required.
- EF6 database initialisation/migrations run against the configured SQL Server on first use; make sure the login has the required rights.
- Memory limit 1Gi: .NET GC honours container limits automatically (workstation GC configured).
