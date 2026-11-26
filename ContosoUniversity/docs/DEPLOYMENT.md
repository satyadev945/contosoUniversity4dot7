# ContosoUniversity - Deployment Guide (AWS EKS)

ASP.NET Core MVC (.NET 8) + EF Core (SQL Server). Listens on **8080**, health endpoint **/health**.

> Base image note: the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows / .NET Framework image and cannot run this net8.0 application. The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux).

## 1. Prerequisites
Docker, AWS CLI v2 (configured), kubectl, eksctl (optional), an EKS cluster with the AWS Load Balancer Controller installed, a reachable SQL Server instance, and IAM permissions for ECR and `eks:DescribeCluster`.

## 2. Local run (Docker Compose)
Run from `ContosoUniversity/`:
```
export SCHOOL_CONNECTION_STRING="Server=host;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
```
Browse http://localhost:8080 ; health: http://localhost:8080/health. Only the app is defined; SQL Server is external.

## 3. Build and push
Linux/macOS: `./ContosoUniversity/scripts/build-push.sh` ; Windows: `ContosoUniversity\scripts\build-push.bat`.
The script prompts for tag (default `latest`), registry (1 ECR / 2 Docker Hub), creates the ECR repository if missing, builds with `docker build -f ContosoUniversity/Dockerfile .` (context = repo root) and pushes.

## 4. Deploy to EKS
`./ContosoUniversity/scripts/deploy-image.sh` (or `.bat`). It prompts for region, cluster, image URI, namespace, environment and the SQL connection string, renders the manifests in a temp folder (`{{IMAGE_URI}}`, `{{NAMESPACE}}`, `{{ASPNETCORE_ENVIRONMENT}}`, `{{CONNECTION_STRING_B64}}`), runs `aws eks update-kubeconfig`, applies namespace -> deployment -> service -> ingress, waits for rollout and prints the ALB address.

Manifests (`kubernetes/`): `namespace.yaml`; `deployment.yaml` (Secret for the connection string + Deployment, 2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health`); `service.yaml` (ClusterIP 80 -> 8080); `ingress.yaml` (ALB, ip target type, health check `/health`).

## 5. Operations
- Status: `kubectl get pods,svc,ingress -n contosouniversity`
- Logs: `kubectl logs deploy/contosouniversity -n contosouniversity`
- Scale: `kubectl scale deploy/contosouniversity --replicas=4 -n contosouniversity` or create an HPA (`kubectl autoscale deploy/contosouniversity --cpu-percent=70 --min=2 --max=6 -n contosouniversity`)
- Update: re-run deploy script with a new image tag (rolling update, maxUnavailable 0)
- Rollback: `kubectl rollout undo deploy/contosouniversity -n contosouniversity`

## 6. Troubleshooting
- Pods `CrashLoopBackOff`: the app throws if `ConnectionStrings__SchoolContext` is empty; check logs and the secret.
- `ImagePullBackOff`: node role needs ECR read access; verify the image URI/tag.
- Not ready: probe `/health` only checks the process; DB problems appear in logs (EF retries on failure).
- Ingress has no address: confirm AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`) and subnets are tagged for ELB.
- 502 from ALB: check service endpoints `kubectl get endpoints -n contosouniversity`.

## 7. Configuration and security
- Configuration via environment variables: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `ConnectionStrings__SchoolContext`.
- The database is created on startup with `EnsureCreated()` and seeded; for production prefer EF migrations.
- Container runs as non-root (`USER $APP_UID`), no extra packages installed, no HEALTHCHECK in the image.
- Store the connection string in AWS Secrets Manager (External Secrets / CSI driver) for production; enable HTTPS on the ALB with an ACM certificate (`alb.ingress.kubernetes.io/certificate-arn`).
- Restrict SQL Server access to the cluster's security group.

## 8. .NET notes
Memory limit 1Gi: .NET GC honours container limits automatically. Consider ReadyToRun publishing for faster start-up and Application Insights/Serilog for observability.
