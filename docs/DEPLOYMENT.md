# ContosoUniversity – Deployment Guide (Docker and AWS EKS)

## Application summary
- ASP.NET Core MVC, .NET 8 (`net8.0`), Entity Framework Core 8 with SQL Server
- Listens on port **8080** (`ASPNETCORE_URLS=http://+:8080`), health endpoint `/health`
- Requires a SQL Server connection string via `ConnectionStrings__SchoolContext`
- `Database__AutoInitialize` (default `true`) creates and seeds the database on startup

> Base image note: the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows .NET Framework image and cannot run this net8.0 application (nor run on standard Linux EKS nodes). The Dockerfile therefore uses `mcr.microsoft.com/dotnet/aspnet:8.0`.

## Prerequisites
Docker, AWS CLI v2 (configured), kubectl, eksctl (optional), an EKS cluster with the AWS Load Balancer Controller installed, a reachable SQL Server, and IAM permissions for ECR and `eks:DescribeCluster`.

## Local run with Docker Compose
```bash
export SCHOOL_DB_CONNECTION="Server=host.docker.internal;Database=ContosoUniversity;User Id=sa;Password=<pwd>;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```

## Build and push
From the repository root: `./scripts/build-push.sh` (or `scripts\build-push.bat`). Choose ECR or Docker Hub; the script sanitizes the image name and tag (default `latest`), creates the ECR repository if needed, builds and pushes.

## Deploy to EKS
```bash
./scripts/deploy-image.sh     # or scripts\deploy-image.bat
```
The script prompts for region, cluster, image URI, the connection string and AutoInitialize; runs `aws eks update-kubeconfig`; renders `{{IMAGE_URI}}` and `{{DATABASE_AUTOINITIALIZE}}` into a temp copy of the manifests; creates the `contosouniversity-secrets` Secret; applies namespace, deployment, service, ingress; and waits for rollout.

Manifests (`kubernetes/`): `namespace.yaml` (contosouniversity), `deployment.yaml` (2 replicas, 250m/512Mi requests, 500m/1Gi limits, startup/liveness/readiness probes on `/health`), `service.yaml` (ClusterIP 80 -> 8080), `ingress.yaml` (ALB, internet-facing; edit the host `contosouniversity.example.com`).

## Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## Troubleshooting
- Pods CrashLoopBackOff: `kubectl logs -l app=contosouniversity -n contosouniversity` – usually a missing/invalid connection string or unreachable SQL Server (check security groups between nodes and the database).
- ImagePullBackOff: verify the image URI and that node IAM role has ECR read access.
- Readiness failing: `kubectl describe pod`; the app needs to finish DB initialization (startup probe allows ~3 minutes).
- No ingress address: confirm the AWS Load Balancer Controller is installed and subnets are tagged; `kubectl describe ingress -n contosouniversity`.
- Service issues: `kubectl get endpoints -n contosouniversity`.

## Configuration and security
- Never bake secrets into the image; the connection string is stored in a Kubernetes Secret (consider AWS Secrets Manager with External Secrets).
- Container runs as the non-root `app` user; no extra packages are installed; no HEALTHCHECK in the image (Kubernetes probes are used).
- Terminate TLS at the ALB by adding an ACM certificate annotation (`alb.ingress.kubernetes.io/certificate-arn`) and a 443 listener.
- Use `Database__AutoInitialize=false` in production once the schema exists.
- Data protection keys are in-memory per pod; configure a shared key store if you add auth/antiforgery across replicas.
