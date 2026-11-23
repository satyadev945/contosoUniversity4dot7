# ContosoUniversity - Deployment Guide (Docker and AWS EKS)

## 1. Overview
- ASP.NET Core MVC on .NET 8 (`net8.0`), Entity Framework 6.5.1 (SQL Server).
- Kestrel listens on port **8080** (`ASPNETCORE_URLS=http://+:8080`).
- Health endpoint: `/health` (ASP.NET Core health checks), used by Kubernetes probes and the ALB.
- Database connection string is read from env var `ConnectionStrings__SchoolContext` (fallback `SCHOOL_DB_CONNECTION_STRING`).

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows
> .NET Framework image and cannot run this `net8.0` app. The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:8.0`
> (Linux). To honour the 4.7.2 image the application would first have to be retargeted to .NET Framework.

## 2. Prerequisites
Docker 24+, AWS CLI v2, kubectl, eksctl (optional), an EKS cluster with the **AWS Load Balancer Controller**
installed, IAM permissions (`ecr:*` for the repo, `eks:DescribeCluster`), and a reachable SQL Server instance.

## 3. Local run with Docker Compose
```bash
cd ContosoUniversity
export SCHOOL_DB_CONNECTION_STRING="Server=host;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```
The compose file contains only the application; the database is external.

## 4. Build and push
Run from the **repository root** (the Docker build context must be the root):
```bash
bash ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat            # Windows
```
Choose AWS ECR (repository auto-created) or Docker Hub. Tags are lowercased/sanitised; default `latest`.

## 5. Deploy to EKS
```bash
bash ContosoUniversity/scripts/deploy-image.sh      # or deploy-image.bat
```
The script prompts for region, cluster, image URI, ingress host and (optionally) the DB connection string.
It runs `aws eks update-kubeconfig`, replaces `{{IMAGE_URI}}`/`{{INGRESS_HOST}}` in a temp copy of the manifests,
creates the `contosouniversity-secrets` Secret, applies namespace -> deployment -> service -> ingress and waits for rollout.

Manifests (`kubernetes/`): `namespace.yaml`, `deployment.yaml` (2 replicas, 250m/512Mi requests, 500m/1Gi limits,
startup/liveness/readiness probes on `/health`, runs as UID 1654), `service.yaml` (ClusterIP 80 -> 8080),
`ingress.yaml` (ALB, internet-facing, IP target type).

## 6. Troubleshooting
- `kubectl get pods -n contosouniversity`, `kubectl describe pod <pod> -n contosouniversity`, `kubectl logs <pod> -n contosouniversity`
- ImagePullBackOff: node role needs ECR read access; verify image URI.
- CrashLoop / DB errors: check the secret `kubectl get secret contosouniversity-secrets -n contosouniversity` and SQL Server security groups.
- Ingress has no address: confirm the AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`) and subnets are tagged.
- Probes failing: the first request may run EF initializers; the startupProbe allows ~150 s.

## 7. Scaling and management
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 8. Configuration and security
- Never bake connection strings into the image; use the Kubernetes Secret (consider AWS Secrets Manager + External Secrets).
- Container runs as non-root; no extra packages are installed; no HEALTHCHECK instruction (Kubernetes handles probes).
- Add HTTPS by attaching an ACM certificate annotation (`alb.ingress.kubernetes.io/certificate-arn`) and listening on 443.
- Multiple replicas: ASP.NET data-protection keys are per-pod unless persisted to shared storage.
