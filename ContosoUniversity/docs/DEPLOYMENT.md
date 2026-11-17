# ContosoUniversity - Deployment Guide (Docker & AWS EKS)

## 1. Application summary
- ASP.NET Core MVC app, `net8.0`, Entity Framework Core 8 (SQL Server provider).
- Listens on **8080** (`ASPNETCORE_URLS=http://+:8080`, non-root friendly).
- Health endpoint: `GET /health` (ASP.NET Core health checks).
- Requires an external SQL Server; connection string via `ConnectionStrings__SchoolContext` (the app throws at startup if empty).
- Database is created/seeded on startup by `DbInitializer`; set `SKIP_DB_INIT=true` to skip.
- **Base image note:** the requested `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and cannot run a `net8.0` app. The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux).

## 2. Prerequisites
Docker, AWS CLI v2, kubectl, (optional) eksctl; IAM permissions for ECR (`ecr:*` on the repo) and EKS (`eks:DescribeCluster`) plus Kubernetes RBAC on the cluster. The cluster needs the **AWS Load Balancer Controller** installed for the Ingress. A reachable SQL Server (e.g. RDS) from the cluster VPC.

## 3. Local run with Docker Compose
From `ContosoUniversity/`:
```bash
export DB_HOST=<host> DB_PORT=1433 DB_NAME=ContosoUniversity DB_USER=<user> DB_PASSWORD=<password>
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
Compose contains only the application service; SQL Server is external.

## 4. Build and push
Run from the repository root (`AppDBMono_CS1`, the Docker build context):
```bash
./ContosoUniversity/scripts/build-push.sh      # Linux/macOS
ContosoUniversity\scripts\build-push.bat       # Windows
```
Choose AWS ECR or Docker Hub; the scripts sanitize names/tags (lowercase, hyphenated, default `latest`), create the ECR repo if missing, build with `docker build -f ContosoUniversity/Dockerfile .` and push. Note the resulting image URI.

## 5. Deploy to EKS
```bash
./ContosoUniversity/scripts/deploy-image.sh     # or deploy-image.bat
```
Prompts: region, cluster name, image URI, `ASPNETCORE_ENVIRONMENT`, connection string, `SKIP_DB_INIT`. The script fills placeholders in a temporary copy of `kubernetes/*.yaml` (`{{IMAGE_URI}}`, `{{ASPNETCORE_ENVIRONMENT}}`, `{{CONNECTION_STRING}}`, `{{SKIP_DB_INIT}}`), runs `aws eks update-kubeconfig`, applies manifests in order, waits for the rollout and prints resources.

Manifests (`kubernetes/`):
| File | Purpose |
|---|---|
| namespace.yaml | `contosouniversity` namespace |
| deployment.yaml | Secret (connection string) + Deployment (2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness probes on `/health`, runs as UID 1654) |
| service.yaml | ClusterIP 80 -> 8080 |
| ingress.yaml | ALB internet-facing ingress, health check `/health`; change host `contosouniversity.example.com` |

Get the URL: `kubectl get ingress -n contosouniversity`.

## 6. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6   # needs metrics-server
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout history deployment/contosouniversity -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```
Rolling updates use maxUnavailable 0, so no downtime.

## 7. Troubleshooting
- **CrashLoopBackOff / "Connection string 'SchoolContext' not found"**: the Secret value is empty; check `kubectl get secret contosouniversity-secrets -n contosouniversity -o yaml`.
- **Startup SQL errors**: verify security groups allow pod -> SQL Server on 1433 and credentials; `kubectl logs -l app=contosouniversity -n contosouniversity`.
- **Pods not Ready**: `kubectl describe pod`, ensure `/health` returns 200.
- **ImagePullBackOff**: node role needs ECR read access (or add imagePullSecrets for Docker Hub private repos).
- **Ingress has no address**: confirm AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`), subnets are tagged for ELB, check `kubectl describe ingress`.
- **Multiple replicas + DB init**: concurrent `DbInitializer` runs on first start can race; start with 1 replica or set `SKIP_DB_INIT=true` after first initialization.

## 8. Configuration
| Variable | Description |
|---|---|
| ConnectionStrings__SchoolContext | SQL Server connection string (Secret) |
| ASPNETCORE_ENVIRONMENT | Default Production |
| ASPNETCORE_URLS | `http://+:8080` |
| SKIP_DB_INIT | `true` skips DB create/seed |

## 9. Security
- Container runs as non-root (`USER $APP_UID`, 1654) with no extra packages and no HEALTHCHECK instruction.
- Do not commit connection strings; for production prefer AWS Secrets Manager with External Secrets / CSI driver instead of the generated Secret.
- Terminate TLS at the ALB (add ACM certificate annotations and a 443 listener).
- Restrict the SQL Server to cluster security groups; use a least-privilege DB login.

## 10. .NET notes
- `InvariantGlobalization` is enabled (no ICU needed). Container memory limit is honored by the .NET GC.
- Consider `ReadyToRun` publishing for faster startup, and Serilog/CloudWatch for logging.
