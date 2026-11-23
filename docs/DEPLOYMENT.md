# ContosoUniversity – Deployment Guide (Docker & AWS EKS)

## 1. Overview
- Application: ASP.NET Core MVC (net8.0), Entity Framework 6 against SQL Server
- Container: Linux, multi-stage build (`mcr.microsoft.com/dotnet/sdk:8.0` -> `mcr.microsoft.com/dotnet/aspnet:8.0`), non-root (UID 1654)
- Port: 8080 (`ASPNETCORE_URLS=http://+:8080`); health endpoint: `/health`
- Note on base image: `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` was requested, but it is a Windows .NET Framework image that cannot run a net8.0 app. The Linux ASP.NET Core 8.0 runtime image is used.

## 2. Prerequisites
Docker 20.10+, AWS CLI v2, kubectl, eksctl (optional), an AWS account with IAM permissions for ECR (`ecr:*` on the repo) and EKS (`eks:DescribeCluster`), an EKS cluster with the **AWS Load Balancer Controller** installed, and a reachable SQL Server.

## 3. Local development with Docker Compose
Run from the repository root:
```bash
export DB_HOST=host.docker.internal DB_NAME=ContosoUniversity DB_USER=sa DB_PASSWORD='<password>'
docker compose up --build
```
Browse to http://localhost:8080 and http://localhost:8080/health. Alternatively set `CONNECTION_STRING` to a full SQL Server connection string. Only the application is defined; the database is external.

## 4. Build and push the image
```bash
./scripts/build-push.sh      # Linux/macOS
scripts\build-push.bat       # Windows
```
Choose AWS ECR or Docker Hub; tags are sanitized (lowercase, default `latest`). ECR repositories are created automatically. Build command used: `docker build -f ContosoUniversity/Dockerfile -t <image> .` (context = repository root).

## 5. EKS setup
```bash
eksctl create cluster --name contoso --region us-east-1 --nodes 2
aws eks update-kubeconfig --region us-east-1 --name contoso
kubectl cluster-info
```
Install the AWS Load Balancer Controller (Helm) so the ALB `Ingress` is provisioned. Ensure nodes can pull from ECR (default node role policy `AmazonEC2ContainerRegistryReadOnly`).

## 6. Deploy to EKS
```bash
./scripts/deploy-image.sh    # or scripts\deploy-image.bat
```
The script prompts for region, cluster, image URI and DB settings, substitutes `{{IMAGE_URI}}`, `{{DB_HOST}}`, `{{DB_NAME}}`, `{{DB_USER}}` in a temporary copy of the manifests (repository files stay unchanged), creates the `contosouniversity-secrets` Secret (DB_PASSWORD, CONNECTION_STRING), applies namespace -> deployment -> service -> ingress, waits for rollout and prints the ALB address.

| Manifest | Purpose |
|---|---|
| `kubernetes/namespace.yaml` | `contosouniversity` namespace |
| `kubernetes/deployment.yaml` | 2 replicas, resources 250m/512Mi – 500m/1Gi, HTTP probes on `/health:8080` |
| `kubernetes/service.yaml` | ClusterIP 80 -> 8080 |
| `kubernetes/ingress.yaml` | Internet-facing ALB (target-type ip), health check `/health` |

## 7. Configuration
| Variable | Description |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__SchoolContext` / `CONNECTION_STRING` | Full SQL Server connection string (takes precedence) |
| `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Used when no full connection string is provided |

## 8. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6   # needs metrics-server
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout status deployment/contosouniversity -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 9. Troubleshooting
- Pods `ImagePullBackOff`: verify image URI, ECR permissions on the node role, region.
- Pods `CrashLoopBackOff` / not ready: `kubectl logs -l app=contosouniversity -n contosouniversity`; check DB connectivity (security groups between EKS nodes and SQL Server) and credentials.
- Service issues: `kubectl get endpoints -n contosouniversity`; `kubectl port-forward svc/contosouniversity-service 8080:80 -n contosouniversity`.
- Ingress has no ADDRESS: check the controller `kubectl logs -n kube-system deploy/aws-load-balancer-controller`, subnet tags (`kubernetes.io/role/elb=1`), and `kubectl describe ingress`.
- ALB targets unhealthy: confirm `/health` returns 200 and pod security group allows ALB traffic on 8080.

## 10. Security
- Container runs as non-root (UID 1654), privilege escalation disabled, all capabilities dropped.
- Credentials live in a Kubernetes Secret; consider AWS Secrets Manager + External Secrets Operator.
- Add HTTPS: set `alb.ingress.kubernetes.io/certificate-arn`, listen-ports `[{"HTTP":80},{"HTTPS":443}]` and `ssl-redirect`.
- Restrict SQL Server access to the cluster security group; enable ECR image scanning.

## 11. .NET notes
- Entity Framework 6 on .NET 8 is supported via the `EntityFramework` 6.4.4 package; `InvariantGlobalization` is enabled so no ICU libraries are required.
- Tune memory with `DOTNET_GCHeapHardLimitPercent` if needed; limits are 1Gi per pod.
- Dockerfile contains no HEALTHCHECK; Kubernetes probes handle health.
