# ContosoUniversity - Deployment Guide (Docker and AWS EKS)

## 1. Application overview
- ASP.NET Core MVC, `net8.0`, Entity Framework 6.4.4 (SQL Server via System.Data.SqlClient).
- Listens on **port 8080** (`ASPNETCORE_URLS=http://+:8080`). Health endpoint: **`/health`** (`MapHealthChecks`).
- No `appsettings.json`; the DB connection string comes from the environment variable `ConnectionStrings__SchoolContext` (or `DB_CONNECTION_STRING`).
- Base image: the requested `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image that cannot run a `net8.0` app, so `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux) is used.

## 2. Prerequisites
Docker 24+, AWS CLI v2, kubectl, (optional) eksctl, an EKS cluster with the **AWS Load Balancer Controller** installed, an ECR/Docker Hub account, and a reachable SQL Server. IAM permissions: `ecr:*` for the repository, `eks:DescribeCluster`, and a Kubernetes RBAC mapping for your IAM identity.

## 3. Local development with Docker Compose
Run from the `ContosoUniversity` folder:
```bash
export DB_CONNECTION_STRING="Server=host;Database=School;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
Only the application service is defined; the database is external.

## 4. Build and push
Run from the **repository root** (docker build context is `.`):
```bash
bash ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat          :: Windows
```
Choose 1 (AWS ECR; repository is auto-created) or 2 (Docker Hub). Tags are lower-cased and hyphenated; empty tag becomes `latest`.

## 5. EKS setup
```bash
eksctl create cluster --name contoso --region us-east-1 --nodes 2     # if you need one
aws eks update-kubeconfig --region us-east-1 --name contoso
kubectl cluster-info
```
Install the AWS Load Balancer Controller (Helm) so the ALB ingress is provisioned.

## 6. Deploy
```bash
bash ContosoUniversity/scripts/deploy-image.sh     # or deploy-image.bat
```
The script prompts for region, cluster, image URI, namespace (default `contosouniversity`) and the optional DB connection string (stored as Secret `contosouniversity-secrets`). It copies the manifests to a temp dir, replaces `{{IMAGE_URI}}` and `{{NAMESPACE}}`, then applies: namespace -> secret -> deployment -> service -> ingress, waits for rollout, and prints the ALB URL.

Manifests (`kubernetes/`):
- `namespace.yaml` - namespace.
- `deployment.yaml` - 2 replicas, requests 250m/512Mi, limits 500m/1Gi, non-root (UID 1654), startup/liveness/readiness probes on `/health:8080`.
- `service.yaml` - ClusterIP 80 -> 8080.
- `ingress.yaml` - internet-facing ALB, `target-type: ip`, health check `/health`.

## 7. Configuration
| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__SchoolContext` | SQL Server connection string (Secret) |
| `webpages__Enabled` | legacy flag, `false` |

## 8. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6   # needs metrics-server
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 9. Troubleshooting
- Pods `ImagePullBackOff`: verify the URI, and that the node role has `AmazonEC2ContainerRegistryReadOnly`.
- Pods `CrashLoopBackOff`/not ready: `kubectl logs deploy/contosouniversity -n contosouniversity`; check the connection string and DB network access (security groups).
- Ingress has no address: confirm AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`) and subnets are tagged for ELB; `kubectl describe ingress -n contosouniversity`.
- 502/503 from ALB: confirm target group health check path `/health` and the Service selector matches the pod labels.
- Note: the app uses EF6 on .NET 8; if the DB is unreachable, pages that query data will fail while `/health` still returns healthy.

## 10. Security
- Container runs as non-root (`app`, UID 1654), privilege escalation disabled, all capabilities dropped.
- Keep connection strings in Secrets (consider AWS Secrets Manager + External Secrets), restrict DB access to the node/pod security groups.
- Terminate TLS at the ALB: add an ACM certificate (`alb.ingress.kubernetes.io/certificate-arn`) and a 443 listener.
- Use ECR image scanning and pin image tags instead of `latest` in production.

## 11. .NET notes
- Server GC disabled by default; memory limit of 1Gi is honored by the runtime automatically.
- `InvariantGlobalization` is enabled in the project, so no ICU packages are required.
