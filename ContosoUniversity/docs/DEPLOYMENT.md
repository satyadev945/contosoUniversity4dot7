# ContosoUniversity – Deployment Guide (AWS EKS)

## 1. Application summary
- ASP.NET Core MVC, .NET 8 (`net8.0`), Kestrel on port **8080**, health endpoint **`/health`**.
- External dependency: SQL Server, configured via `ConnectionStrings__SchoolContext` or `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`.
- Base image: `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux). The requested `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows/.NET Framework image that cannot run a `net8.0` app (nor on Linux EKS nodes), so it was not used.

## 2. Prerequisites
Docker, AWS CLI v2 (configured), kubectl, eksctl (optional), an EKS cluster with the AWS Load Balancer Controller, a reachable SQL Server, and IAM permissions for ECR and `eks:DescribeCluster`.

## 3. Local development
From the repository root (`appcsharpmup`):
```
export DB_HOST=<sql-host> DB_PASSWORD=<password>
docker compose -f ContosoUniversity/docker-compose.yml up --build
```
Browse http://localhost:8080 and http://localhost:8080/health.

## 4. Build and push
From the repository root run `ContosoUniversity/scripts/build-push.sh` (or `.bat`). Choose AWS ECR or Docker Hub; the script sanitizes the tag, creates the ECR repository if needed, builds with `docker build -f ContosoUniversity/Dockerfile .` and pushes.

## 5. Deploy to EKS
Run `ContosoUniversity/scripts/deploy-image.sh` (or `.bat`). It prompts for region, cluster, image URI and DB settings, replaces `{{IMAGE_URI}}`, `{{DB_*}}` placeholders in a temporary copy of the manifests, runs `aws eks update-kubeconfig`, and applies namespace → deployment → service → ingress, then waits for the rollout.

Manifests (`ContosoUniversity/kubernetes`):
- `namespace.yaml` – namespace `contosouniversity`
- `deployment.yaml` – 2 replicas, requests 250m/512Mi, limits 500m/1Gi, startup/liveness/readiness probes on `/health:8080`
- `service.yaml` – ClusterIP 80 → 8080
- `ingress.yaml` – ALB internet-facing ingress; change host `contosouniversity.example.com`

## 6. Troubleshooting
- Pods `CrashLoopBackOff`: `kubectl logs -n contosouniversity deploy/contosouniversity`; check DB_* values and SQL Server network access (security groups).
- `ImagePullBackOff`: verify the image URI and that node role has ECR read access.
- Readiness failing: the app initializes the database on startup; confirm DB connectivity.
- Ingress has no address: confirm AWS Load Balancer Controller is installed (`kubectl get pods -n kube-system`) and subnets are tagged.

## 7. Scaling and management
- Scale: `kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4`
- HPA: `kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6`
- Rolling update: re-run deploy script with a new tag; rollback with `kubectl rollout undo deployment/contosouniversity -n contosouniversity`.

## 8. Configuration and security
- Move DB credentials to a Kubernetes Secret / AWS Secrets Manager (External Secrets) for production instead of plain env values.
- Container runs as non-root (`USER $APP_UID`), port 8080 (>1024).
- Add HTTPS with an ACM certificate annotation (`alb.ingress.kubernetes.io/certificate-arn`) and listen-ports `443`.
- `TrustServerCertificate=True` is used for the connection string fallback; use a trusted certificate in production.

## 9. .NET notes
- Set `DOTNET_gcServer=0` (workstation GC) for small containers; memory limits are respected by the runtime automatically.
- `InvariantGlobalization` is enabled in the project, so no ICU libraries are needed.
