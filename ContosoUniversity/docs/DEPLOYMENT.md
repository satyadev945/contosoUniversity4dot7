# ContosoUniversity – Deployment Guide (Docker & AWS EKS)

ContosoUniversity is an ASP.NET Core MVC application (net8.0, Kestrel, EF Core + SQL Server).

| Item | Value |
|------|-------|
| Container port | 8080 (`ASPNETCORE_URLS=http://+:8080`) |
| Health endpoint | `/health` (`MapHealthChecks`) |
| Database | External SQL Server, set via `ConnectionStrings__SchoolContext` |
| Build image / runtime image | `mcr.microsoft.com/dotnet/sdk:8.0` / `mcr.microsoft.com/dotnet/aspnet:8.0` |

> **Base image note:** the requested base image `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` is a Windows
> .NET Framework image and cannot run a `net8.0` application. The Linux ASP.NET Core 8.0 runtime image is used instead.

All paths below are relative to the repository root (`AppCsharpMono`). Scripts must be run from there.

## 1. Prerequisites
- Docker 24+ (BuildKit)
- AWS CLI v2 configured (`aws configure`) with permissions for ECR and EKS
- kubectl, eksctl (optional, for creating a cluster)
- An EKS cluster with the **AWS Load Balancer Controller** installed (required for the ALB Ingress)
- A reachable SQL Server instance (e.g. Amazon RDS for SQL Server)

## 2. Local run with Docker Compose
```bash
cd ContosoUniversity
export CONNECTION_STRING="Server=host.docker.internal;Database=ContosoUniversity;User Id=sa;Password=<password>;TrustServerCertificate=True"
docker compose up --build
```
Open http://localhost:8080 – health check at http://localhost:8080/health.
The compose file contains only the application; the database is external.

## 3. Build and push the image
```bash
./ContosoUniversity/scripts/build-push.sh        # Linux/macOS
ContosoUniversity\scripts\build-push.bat         # Windows
```
The script asks for a tag (default `latest`) and registry (1 = AWS ECR, 2 = Docker Hub), creates the ECR repository if
needed, builds with `docker build -f ContosoUniversity/Dockerfile .` and pushes. It prints the final image URI.

## 4. Deploy to EKS
```bash
./ContosoUniversity/scripts/deploy-image.sh      # or deploy-image.bat
```
The script prompts for AWS region, cluster name, image URI, ingress host and (optionally) the SQL Server connection string.
It then: updates kubeconfig, verifies connectivity, applies `namespace → (secret) → deployment → service → ingress`, waits
for the rollout and prints the ALB address. The connection string is stored in the Kubernetes Secret
`contosouniversity-secrets` (key `ConnectionStrings__SchoolContext`), never in the manifest.

### Manifests (`ContosoUniversity/kubernetes`)
| File | Purpose |
|------|---------|
| `namespace.yaml` | Namespace `contosouniversity` |
| `deployment.yaml` | 2 replicas, rolling update, startup/readiness/liveness probes on `/health`, requests 250m/512Mi, limits 500m/1Gi, non-root (UID 1654) |
| `service.yaml` | ClusterIP, port 80 → 8080 |
| `ingress.yaml` | ALB (`internet-facing`, `target-type: ip`), health check `/health`, sticky sessions |

Placeholders `{{IMAGE_URI}}` and `{{INGRESS_HOST}}` are substituted in a temporary copy – the templates are not modified.

## 5. Configuration
| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__SchoolContext` | SQL Server connection string (Secret) |
| `TZ` | `UTC` |

The app creates/seeds the database on start (`SchoolInitializer`); the SQL login needs the matching permissions. If the
database is unreachable the error is logged and the app keeps running (`/health` stays healthy, it does not test the DB).

## 6. Scaling and operations
```bash
kubectl scale deployment/contosouniversity -n contosouniversity --replicas=4
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6   # needs metrics-server
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity       # rolling update
kubectl rollout history deployment/contosouniversity -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## 7. Troubleshooting
| Symptom | Checks |
|---------|--------|
| `ImagePullBackOff` | Image URI/tag correct? Node IAM role has `AmazonEC2ContainerRegistryReadOnly`? Docker Hub private repo needs an imagePullSecret |
| `CrashLoopBackOff` | `kubectl logs deployment/contosouniversity -n contosouniversity` |
| Pods not Ready | `kubectl describe pod` – probe failures on `/health`, port 8080 |
| Ingress has no ADDRESS | AWS Load Balancer Controller installed? `kubectl describe ingress -n contosouniversity`; subnets tagged for ELB |
| 502/503 from ALB | Target group health in the AWS console; security groups allow ALB → pod port 8080 |
| DB errors in logs | Secret present? `kubectl get secret contosouniversity-secrets -n contosouniversity`; SQL Server security group allows the nodes |
| Antiforgery/validation errors | Data-protection keys are per pod; keep ALB stickiness or share keys (see recommendations) |

## 8. Security
- Runs as non-root (`app`, UID 1654), privilege escalation disabled, all capabilities dropped.
- Keep credentials in Secrets (consider AWS Secrets Manager + External Secrets Operator).
- Terminate TLS on the ALB: add an ACM certificate (`alb.ingress.kubernetes.io/certificate-arn`) and `listen-ports` with HTTPS.
- Do not use the placeholder `sa` password from `appsettings.json` in real environments; use a least-privilege SQL login.
- Scan images (ECR scanning) and pin base images by digest where possible.

## 9. .NET notes
- `InvariantGlobalization` is enabled in the csproj, so no ICU libraries are needed in the image.
- For memory-constrained pods .NET respects cgroup limits automatically; tune with `DOTNET_GCHeapHardLimitPercent` if required.
- Static assets are served by Kestrel from `wwwroot`.
