# ContosoUniversity – Deployment Guide (Docker / AWS EKS)

ASP.NET Core MVC app (.NET 8, EF Core SQL Server). Listens on **8080**; health endpoint **/health**.
The requested base image `mcr.microsoft.com/dotnet/framework/runtime:4.7.2` is a Windows .NET Framework image and
cannot host a net8.0 app, so `mcr.microsoft.com/dotnet/aspnet:8.0` is used.

## Prerequisites
Docker, AWS CLI v2 (configured), kubectl, eksctl (optional), an EKS cluster with the AWS Load Balancer Controller installed,
IAM permissions for ECR and `eks:DescribeCluster`, and a reachable SQL Server instance.

## Local development
```
cd ContosoUniversity
export SCHOOL_CONNECTION_STRING="Server=host.docker.internal,1433;Database=ContosoUniversity;User Id=sa;Password=...;TrustServerCertificate=True"
docker compose up --build
```
Browse http://localhost:8080, health: http://localhost:8080/health. Only the app is in compose; SQL Server is external.

## Build and push
From the repository root:
```
ContosoUniversity/scripts/build-push.sh      # or ContosoUniversity\scripts\build-push.bat
```
Choose ECR or Docker Hub; the ECR repository is created automatically. Build context is the repo root
(`docker build -f ContosoUniversity/Dockerfile .`).

## Deploy to EKS
```
ContosoUniversity/scripts/deploy-image.sh     # or deploy-image.bat
```
The script prompts for region, cluster, image URI and the connection string, substitutes `{{IMAGE_URI}}`, `{{NAMESPACE}}`,
`{{CONNECTION_STRING}}` in a temporary copy of the manifests, runs `aws eks update-kubeconfig`, and applies
namespace → deployment → service → ingress, then waits for rollout.

Manifests (`kubernetes/`): namespace `contosouniversity`; Deployment (2 replicas, 250m/512Mi requests, 500m/1Gi limits,
startup/liveness/readiness on `/health`, non-root UID 1654); ClusterIP Service 80→8080; ALB Ingress (internet-facing, ip targets).

## Configuration
| Variable | Purpose |
|---|---|
| ConnectionStrings__SchoolContext | SQL Server connection string |
| ASPNETCORE_ENVIRONMENT | Production |
| ASPNETCORE_URLS | http://+:8080 |

For production, move the connection string into a Kubernetes Secret (or AWS Secrets Manager via External Secrets) and reference it with `secretKeyRef`.
Note the app runs `SchoolInitializer.Initialize` at startup, so the DB login needs rights to create/seed the database.

## Scaling and management
```
kubectl autoscale deployment contosouniversity -n contosouniversity --cpu-percent=70 --min=2 --max=6
kubectl set image deployment/contosouniversity contosouniversity=<new-image> -n contosouniversity
kubectl rollout undo deployment/contosouniversity -n contosouniversity
```

## Troubleshooting
- Pods CrashLoopBackOff / not ready: `kubectl logs deploy/contosouniversity -n contosouniversity`; usually DB connectivity (security groups between EKS nodes and SQL Server) – the startup probe allows ~150s.
- ImagePullBackOff: check ECR permissions on the node role and the image URI.
- Service issues: `kubectl get endpoints -n contosouniversity`.
- No ingress address: confirm AWS Load Balancer Controller is running (`kubectl get pods -n kube-system`), subnets are tagged, `kubectl describe ingress -n contosouniversity`.

## Security
Non-root user, dropped capabilities, no privilege escalation, no secrets in image, use HTTPS on the ALB (add ACM certificate annotations
`alb.ingress.kubernetes.io/certificate-arn` and a 443 listen-port), restrict SQL Server network access.

## .NET notes
Invariant globalization is enabled (no ICU needed). Set `DOTNET_gcServerGarbageCollector=0` / `DOTNET_GCHeapHardLimit` if tuning memory under the 1Gi limit.
