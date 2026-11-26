# ContosoUniversity – Deployment Guide (Docker & AWS EKS)

## 1. Application overview

| Item | Value |
|------|-------|
| Type | ASP.NET Core MVC web app (net8.0, `Microsoft.NET.Sdk.Web`) |
| Data access | Entity Framework 6.5.1 + System.Data.SqlClient (external SQL Server) |
| Port | 8080 (`ASPNETCORE_URLS=http://+:8080`) |
| Health endpoint | `GET /health` (`AddHealthChecks` / `MapHealthChecks`) |
| Config | `appsettings.json`; DB settings via env vars `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` or `ConnectionStrings__SchoolContext` |
| Image | Multi-stage: `mcr.microsoft.com/dotnet/sdk:8.0` -> `mcr.microsoft.com/dotnet/aspnet:8.0`, non-root UID 1654 |

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows / .NET Framework 4.7.2 image and **cannot run a net8.0 application**. The project already targets .NET 8, so the Linux `aspnet:8.0` runtime image is used. If you must use a Framework base image, the application must first be ported back to .NET Framework.

## 2. Prerequisites

- Docker 24+ (Linux containers)
- AWS CLI v2 configured (`aws configure`), kubectl, optionally eksctl
- An EKS cluster with the **AWS Load Balancer Controller** installed (required for the ALB Ingress)
- A reachable SQL Server instance (RDS for SQL Server or similar) and its credentials
- IAM permissions: `ecr:*` (push), `eks:DescribeCluster`, and cluster access mapped to your IAM identity (aws-auth / access entries)

All commands below run from the repository root (`Mono-db`).

## 3. Local run with Docker Compose

```bash
export DB_HOST=<sql-server-host>  DB_NAME=ContosoUniversity  DB_USER=<user>  DB_PASSWORD=<password>
docker compose -f ContosoUniversity/docker-compose.yml up --build
# http://localhost:8080   health: http://localhost:8080/health
```

The compose file contains only the application; the database is external.

## 4. Build and push the image

```bash
bash ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat            # Windows
```

The script prompts for the tag (default `latest`; sanitised to lowercase), registry (1 = AWS ECR, 2 = Docker Hub) and credentials. For ECR it creates the repository if missing. It ends by printing the full image URI.

Equivalent manual build: `docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .`

## 5. EKS setup

```bash
eksctl create cluster --name contoso --region us-east-1 --nodes 2     # if you need a cluster
aws eks update-kubeconfig --region us-east-1 --name contoso
kubectl cluster-info
```

Install the AWS Load Balancer Controller (Helm) per the AWS documentation, then proceed.

## 6. Deploy to EKS

```bash
bash ContosoUniversity/scripts/deploy-image.sh      # or deploy-image.bat on Windows
```

The script prompts for region, cluster name, image URI and DB settings, substitutes `{{IMAGE_URI}}`, `{{DB_HOST}}`, `{{DB_NAME}}`, `{{DB_USER}}` in a temporary copy of the manifests, creates the `contosouniversity-db` Secret (DB password), applies the manifests in order (namespace, deployment, service, ingress), waits for rollout and prints the ALB URL.

| Manifest | Purpose |
|----------|---------|
| `kubernetes/namespace.yaml` | `contosouniversity` namespace |
| `kubernetes/deployment.yaml` | 2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness probes on `/health:8080`, non-root |
| `kubernetes/service.yaml` | ClusterIP, 80 -> 8080 |
| `kubernetes/ingress.yaml` | ALB (`ingressClassName: alb`), internet-facing, target-type ip, health check `/health` |

## 7. Operations

```bash
kubectl get pods,svc,ingress -n contosouniversity
kubectl logs -l app=contosouniversity -n contosouniversity --tail=100
kubectl scale deployment/contosouniversity --replicas=4 -n contosouniversity
kubectl set image deployment/contosouniversity contosouniversity=<new-image-uri> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
kubectl autoscale deployment contosouniversity --cpu-percent=70 --min=2 --max=6 -n contosouniversity   # HPA (needs metrics-server)
```

## 8. Troubleshooting

| Symptom | Checks |
|---------|--------|
| `ImagePullBackOff` | Image URI/tag correct? Node role has `AmazonEC2ContainerRegistryReadOnly`? Same region/account? |
| `CrashLoopBackOff` | `kubectl logs` / `kubectl describe pod`; usually a bad DB connection or a missing env var |
| Pod not Ready | `/health` is reachable only on 8080; check probe events with `kubectl describe pod` |
| Ingress has no address | AWS Load Balancer Controller installed? Subnets tagged for ELB (`kubernetes.io/role/elb=1`)? `kubectl describe ingress -n contosouniversity` |
| 502/504 from ALB | Security group must allow ALB -> pod port 8080; confirm target group health in the AWS console |
| DB connection errors | EKS nodes/pods must reach SQL Server (security group, VPC routing); verify `DB_HOST`, credentials |

## 9. Configuration and security

- Configuration is environment-driven; `ConnectionStrings__SchoolContext` can be set instead of the `DB_*` variables.
- Database password is stored in a Kubernetes Secret; consider AWS Secrets Manager + External Secrets / CSI driver for production.
- Container runs as non-root (UID 1654), privilege escalation disabled, all capabilities dropped.
- TLS terminates at the ALB: add an ACM certificate (`alb.ingress.kubernetes.io/certificate-arn`, listen-ports `[{"HTTPS":443}]`) and a host rule.
- **Multiple replicas and ASP.NET Core Data Protection:** keys are per-pod by default, so antiforgery tokens/cookies may fail when requests hit different pods. Persist keys to a shared store (e.g. S3/SQL/Redis via `PersistKeysTo...`) or enable ALB stickiness.
- EF6 `DropCreateDatabaseIfModelChanges`-style initialisers can recreate databases; verify initializer behaviour before pointing at a production database.

## 10. .NET specific notes

- `InvariantGlobalization=true` is set in the project, so no ICU libraries are needed.
- `DOTNET_GCHeapHardLimitPercent=75` keeps GC within the container memory limit.
- No `HEALTHCHECK` or extra tooling is installed in the image; Kubernetes probes handle health checking.
