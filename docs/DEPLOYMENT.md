# ContosoUniversity – Deployment Guide (Docker & AWS EKS)

## 1. Overview
ContosoUniversity is an ASP.NET Core MVC application (net8.0, Entity Framework 6 on SQL Server). The container listens on **port 8080** and exposes a health endpoint at **/health**. The SQL Server database is external and supplied via environment variable.

| Item | Value |
|------|-------|
| Build / runtime images | `mcr.microsoft.com/dotnet/sdk:8.0` / `mcr.microsoft.com/dotnet/aspnet:8.0` |
| Container port | 8080 (Service port 80) |
| Health endpoint | `/health` |
| Required config | `ConnectionStrings__SchoolContext` (SQL Server connection string) |
| Optional config | `APPLICATIONINSIGHTS_CONNECTION_STRING` |

> **Base image note:** the requested `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and cannot run a net8.0 ASP.NET Core app (nor run on Linux EKS nodes). The Linux `aspnet:8.0` runtime image is used instead.

## 2. Prerequisites
- Docker 24+, AWS CLI v2, kubectl, (optional) eksctl
- IAM permissions: ECR (create/push), `eks:DescribeCluster`, and Kubernetes RBAC access to the cluster
- An EKS cluster with the **AWS Load Balancer Controller** installed (required for the ALB Ingress)
- A reachable SQL Server instance (network path from the EKS VPC)

## 3. Local development with Docker Compose
```bash
export DB_CONNECTION_STRING="Server=host.docker.internal,1433;Database=School;User Id=sa;Password=<pwd>;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
Only the application service is defined; the database is not part of the compose file.

## 4. Build and push the image
Run from the repository root:
```bash
./scripts/build-push.sh        # Linux/macOS
scripts\build-push.bat         # Windows
```
The script prompts for the tag (default `latest`; sanitized to lowercase), registry (1. AWS ECR, 2. Docker Hub) and credentials. For ECR the repository `contosouniversity` is created if missing. Note the printed image URI.

## 5. Deploy to EKS
```bash
./scripts/deploy-image.sh      # or scripts\deploy-image.bat
```
The script prompts for region, cluster name, image URI, DB connection string and optional Application Insights connection string, then:
1. Runs `aws eks update-kubeconfig` and verifies `kubectl cluster-info`
2. Applies `kubernetes/namespace.yaml`
3. Creates/updates secret `contosouniversity-secrets`
4. Applies deployment, service and ingress (with `{{IMAGE_URI}}` substituted in a temp copy)
5. Waits for rollout and prints the ALB hostname

### Manifests
- `namespace.yaml` – namespace `contosouniversity`
- `deployment.yaml` – 2 replicas, non-root (UID 1654), requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`
- `service.yaml` – ClusterIP 80 → 8080
- `ingress.yaml` – internet-facing ALB, `target-type: ip`, health check `/health`

## 6. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6   # needs metrics-server
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity       # rolling update
kubectl rollout undo deployment/contosouniversity -n contosouniversity                                  # rollback
```

## 7. Troubleshooting
| Symptom | Checks |
|---------|--------|
| Pod `CrashLoopBackOff` | `kubectl logs <pod> -n contosouniversity`; missing/invalid DB connection string throws on first request scope |
| `ImagePullBackOff` | Verify image URI; node IAM role needs ECR read permissions |
| Pod not Ready | `kubectl describe pod`; `/health` must return 200; check DB network/security group |
| Ingress has no ADDRESS | Confirm AWS Load Balancer Controller is running; `kubectl describe ingress -n contosouniversity`; subnets need ELB tags |
| 502/504 from ALB | Target group health in the AWS console; security groups must allow ALB → pod port 8080 |

## 8. Configuration & security
- Use .NET configuration env vars (`ConnectionStrings__SchoolContext`); secrets are stored in a Kubernetes Secret (consider AWS Secrets Manager + External Secrets/CSI driver).
- Container runs as non-root, drops all capabilities, no privilege escalation.
- For HTTPS, add an ACM certificate annotation (`alb.ingress.kubernetes.io/certificate-arn`) and `listen-ports` for 443; TLS terminates at the ALB.
- Restrict SQL Server access to the cluster's security group; use a least-privilege SQL login.

## 9. .NET notes
- `InvariantGlobalization` is enabled; workstation GC is used (`DOTNET_gcServer=0`) suited to 1Gi limits.
- Application Insights is enabled when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set.
- EF6 migrations are not run automatically by the container; apply schema changes separately.
