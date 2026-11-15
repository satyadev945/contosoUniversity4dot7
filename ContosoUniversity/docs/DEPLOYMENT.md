# ContosoUniversity – Deployment Guide (Docker and AWS ECS Fargate)

## 1. Application summary

| Item | Value |
|---|---|
| Stack | ASP.NET Core 8.0 MVC (`Microsoft.NET.Sdk.Web`), EF Core 8 + SQL Server |
| Container port | 8080 (`ASPNETCORE_URLS=http://+:8080`) |
| Health endpoint | `/health` (`AddHealthChecks` / `MapHealthChecks`) |
| Config | `appsettings.json`, environment variables (`ConnectionStrings__SchoolContext`, `DB_*`, `DB_INITIALIZE`) |
| Image | Linux, `mcr.microsoft.com/dotnet/sdk:8.0` (build) → `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime) |

> **Base image note:** the request named `mcr.microsoft.com/dotnet/framework/aspnet:4.7.2` as the explicit base image. That is a Windows / .NET Framework 4.7.2 image and cannot run this project (it targets `net8.0`), and ECS Fargate Linux tasks cannot run it. The auto-detected `mcr.microsoft.com/dotnet/aspnet:8.0` is used instead.

The database is **not** part of the container artifacts. Provide an external SQL Server (e.g. Amazon RDS for SQL Server) reachable from the task.

## 2. Files

```
ContosoUniversity/
├── Dockerfile
├── .dockerignore
├── docker-compose.yml          # application service only
├── ecs/task-definition.json
├── ecs/service-definition.json
├── scripts/build-push.sh|.bat
├── scripts/deploy-image.sh|.bat
└── docs/DEPLOYMENT.md
```

## 3. Local development / Docker

```bash
cd ContosoUniversity
export SCHOOL_DB_CONNECTION_STRING="Server=host.docker.internal,1433;Database=ContosoUniversity;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
export DB_INITIALIZE=true     # first run only: creates/seeds the schema through SchoolInitializer
docker compose up --build
# http://localhost:8080  and  http://localhost:8080/health
```

The Dockerfile uses multi-stage build, NuGet restore layer caching, runs as the non-root `app` user, contains no `HEALTHCHECK` and installs no extra packages. Build argument `BASE_REGISTRY` can point to an ECR pull-through cache for MCR images.

## 4. AWS prerequisites

- AWS CLI v2 configured (`aws configure`), plus `jq` for the Linux/macOS deploy script and PowerShell for Windows.
- A VPC with at least two subnets (public subnets if `assignPublicIp` is `ENABLED`, which is the default in the service definition).
- A security group that allows inbound TCP 80 (ALB) and TCP 8080 (task). When using the ALB created by the script, the same security group is attached to both; for tighter control use separate groups.
- IAM role `ecsTaskExecutionRole` with `AmazonECSTaskExecutionRolePolicy`, plus `secretsmanager:GetSecretValue` (or `ssm:GetParameters`) on the connection-string secret.
- A Secrets Manager secret (or SSM parameter) containing the SQL Server connection string, e.g.
  `Server=mydb.xxxx.us-east-1.rds.amazonaws.com,1433;Database=ContosoUniversity;User Id=app;Password=...;TrustServerCertificate=True;`
- The SQL Server security group must allow inbound 1433 from the task security group.

## 5. Build and push

```bash
./scripts/build-push.sh        # Linux/macOS
scripts\build-push.bat         # Windows
```
Choose 1 (ECR) or 2 (Docker Hub). Names/tags are sanitised (lowercase, hyphens, default tag `latest`); the ECR repository `contosouniversity` is auto-created. Note the printed image URI.

## 6. Deploy to ECS Fargate

```bash
./scripts/deploy-image.sh      # or scripts\deploy-image.bat
```
The script prompts for region, cluster, VPC, two subnets, security group, image URI, the secret ARN and whether a load balancer is needed. It then:

1. Creates the cluster (if missing) and CloudWatch log group `/ecs/contosouniversity`.
2. (Optional) Creates an internet-facing ALB, a target group (`target-type ip`, health check `/health` on 8080) and an HTTP:80 listener.
3. Substitutes `{{PLACEHOLDERS}}` in copies of `ecs/*.json`; removes `loadBalancers` and `healthCheckGracePeriodSeconds` when no ALB is used.
4. Registers the task definition and creates the service (or updates it using the full task definition ARN with a forced new deployment).
5. Waits for `services-stable` and prints service status, the log group and the ALB DNS name.

### Task definition (explained)
- `requiresCompatibilities: ["FARGATE"]`, `networkMode: awsvpc`, `cpu: 512`, `memory: 1024` (a valid Fargate combination; increase to 1024/2048 for heavier EF workloads).
- One container `contosouniversity`, port 8080, env `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS=http://+:8080`, `DB_INITIALIZE=false`.
- `ConnectionStrings__SchoolContext` injected through `secrets` from `{{DB_SECRET_ARN}}`.
- `awslogs` driver, group `/ecs/contosouniversity`, prefix `ecs`.
- No container-level health check (the runtime image has no curl/wget); health is checked by the ALB target group on `/health`.

### Service definition (explained)
`FARGATE` launch type, `desiredCount: 2`, awsvpc networking with the subnets/security group, rolling deployment 200% / 50%, ECS managed tags, `propagateTags: SERVICE`, and `tags` (not `serviceTags`).

## 7. Initialising the database
Set `DB_INITIALIZE=true` for a single deployment (edit the environment value in `ecs/task-definition.json`) to run `SchoolInitializer`, then set it back to `false`.

## 8. Troubleshooting

| Symptom | Check |
|---|---|
| Task stops with `CannotPullContainerError` | Image URI/tag exists; subnets have internet/NAT or ECR VPC endpoints; public IP enabled |
| `ResourceInitializationError` retrieving secret | Execution role lacks `secretsmanager:GetSecretValue`, or secret ARN wrong |
| Target group unhealthy | Security group allows 8080 from ALB; `/health` returns 200; app can start (DB reachable) |
| Invalid CPU/memory | Use valid pairs, e.g. 256/512-2048, 512/1024-4096, 1024/2048-8192 |
| App starts then fails with SQL error | Connection string, RDS security group (1433), `TrustServerCertificate` |
| `InvalidParameterException: healthCheckGracePeriodSeconds` | Only valid with a load balancer – the script removes it when no ALB |

Logs: CloudWatch group `/ecs/contosouniversity`, or `aws logs tail /ecs/contosouniversity --follow`.

## 9. Scaling and management
- Service Auto Scaling: register `service/<cluster>/contosouniversity-service` with Application Auto Scaling and a target-tracking policy (CPU 60%).
- Blue/green: use CodeDeploy for ECS with two target groups, or keep rolling updates (default).
- Rollback: `aws ecs update-service --cluster <c> --service contosouniversity-service --task-definition <previous-arn>`.

## 10. Security
- Container runs as non-root; secrets come from Secrets Manager, never baked into the image.
- Terminate TLS at the ALB (add an HTTPS:443 listener with an ACM certificate) – the app itself serves HTTP only.
- Prefer private subnets + NAT and `assignPublicIp: DISABLED` for production.
- Replace `TrustServerCertificate=True` with a trusted RDS CA bundle where possible.

## 11. .NET notes
- `InvariantGlobalization` is enabled (no ICU needed).
- Set `DOTNET_gcServer=0` (workstation GC) for small containers; memory limits are honoured by the runtime automatically.
- Consider ReadyToRun publish (`-p:PublishReadyToRun=true`) to reduce start time, and Serilog / Application Insights for observability.
