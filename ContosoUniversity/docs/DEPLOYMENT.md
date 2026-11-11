# ContosoUniversity – Container & AWS EKS Deployment Guide

## 1. Application summary
- ASP.NET Core MVC, `net8.0`, Entity Framework Core + SQL Server (external database)
- Kestrel listens on **8080** (`ASPNETCORE_URLS=http://+:8080`)
- Health endpoints: `/health` (liveness, no dependencies) and `/health/ready` (readiness, checks database)
- Configuration: `appsettings.json`; the DB connection string is supplied through `SCHOOL_DB_CONNECTION` (or `ConnectionStrings__SchoolContext`)

> **Base image note:** the requested `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework console image. It cannot run a `net8.0` ASP.NET Core app and cannot be scheduled on Linux EKS nodes, so `mcr.microsoft.com/dotnet/aspnet:8.0` is used for the runtime stage.

## 2. Prerequisites
Docker 24+, AWS CLI v2 (configured: `aws configure`), kubectl, eksctl (optional), an EKS cluster with the **AWS Load Balancer Controller** installed, a reachable SQL Server instance, and IAM permissions for ECR (`ecr:*` push actions) and `eks:DescribeCluster`.

## 3. Local development (Docker Compose)
```bash
cd ContosoUniversity
export SCHOOL_DB_CONNECTION="Server=host.docker.internal,1433;Database=SchoolContext;User Id=sa;Password=<pwd>;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  |  http://localhost:8080/health
```
The compose file contains only the application; the database is external.

## 4. Build and push
Run from anywhere (scripts resolve the repo root) – the Docker build context is the repository root:
```bash
./ContosoUniversity/scripts/build-push.sh     # Linux/macOS
ContosoUniversity\scripts\build-push.bat      # Windows
```
Choose 1 (AWS ECR – repo is auto-created) or 2 (Docker Hub), and enter a tag (default `latest`, sanitized to lowercase). Manual equivalent:
```bash
docker build -f ContosoUniversity/Dockerfile -t <registry>/contosouniversity:<tag> .
```

## 5. Deploy to EKS
```bash
./ContosoUniversity/scripts/deploy-image.sh    # or deploy-image.bat
```
The script prompts for region, cluster, image URI, ingress host and the DB connection string, then:
1. `aws eks update-kubeconfig` and `kubectl cluster-info`
2. Applies `namespace.yaml`, creates Secret `contosouniversity-secrets`, applies `deployment.yaml`, `service.yaml`, `ingress.yaml` (placeholders `{{IMAGE_URI}}`, `{{INGRESS_HOST}}` are replaced in a temp copy)
3. Waits for `kubectl rollout status` and prints pods/services/ingress and the ALB address

### Manifests
| File | Purpose |
|---|---|
| `kubernetes/namespace.yaml` | Namespace `contosouniversity` |
| `kubernetes/deployment.yaml` | 2 replicas, non-root (UID 1654), requests 250m/512Mi, limits 500m/1Gi, startup/liveness (`/health`) and readiness (`/health/ready`) probes |
| `kubernetes/service.yaml` | ClusterIP, 80 → 8080 |
| `kubernetes/ingress.yaml` | ALB (internet-facing, target-type ip, health check `/health`) |

## 6. Configuration
| Variable | Description |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `SCHOOL_DB_CONNECTION` | SQL Server connection string (stored in Secret) |
| `Logging__LogLevel__Default` | Logging level override |

Update the secret later: `kubectl create secret generic contosouniversity-secrets -n contosouniversity --from-literal=SCHOOL_DB_CONNECTION='...' --dry-run=client -o yaml | kubectl apply -f -` then `kubectl rollout restart deployment/contosouniversity -n contosouniversity`.

If no connection string is set, the app still starts (liveness passes) but `/health/ready` is unhealthy and pods stay NotReady.

## 7. Scaling & management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6   # needs metrics-server
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout history deployment/contosouniversity -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 8. Troubleshooting
- **Pods Pending:** `kubectl describe pod` – insufficient resources / node group capacity.
- **ImagePullBackOff:** check image URI, and node role has `AmazonEC2ContainerRegistryReadOnly`.
- **Pods Running but not Ready:** `/health/ready` fails → DB unreachable; check security groups between nodes and SQL Server, and the connection string. `kubectl logs deployment/contosouniversity -n contosouniversity`.
- **CrashLoopBackOff:** inspect logs and `kubectl describe pod`; confirm memory limit (1Gi) is sufficient.
- **No ALB address:** `kubectl describe ingress -n contosouniversity`; verify AWS Load Balancer Controller is running (`kubectl get pods -n kube-system | grep load-balancer`) and subnets are tagged.
- **404/503 from ALB:** target group health check uses `/health`; confirm targets are healthy.

## 9. Security
- Container runs as non-root (UID 1654), privilege escalation disabled, all capabilities dropped.
- Connection strings are kept in a Kubernetes Secret; consider AWS Secrets Manager + External Secrets, and add an ACM certificate (`alb.ingress.kubernetes.io/certificate-arn`, listen-ports 443) for HTTPS.
- Use ECR image scanning and pin image tags (avoid `latest` in production).

## 10. .NET notes
- GC is workstation, with `DOTNET_GCHeapHardLimitPercent=75` to respect the container memory limit.
- The aspnet runtime image has no curl/wget; health checking is performed by Kubernetes httpGet probes.
- `UseHttpsRedirection` is not enabled; TLS is terminated at the ALB.
