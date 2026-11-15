using System;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.Migrations;
using System.Threading;
using ContosoUniversity.DAL;
using Google.Cloud.SecretManager.V1;

namespace ContosoUniversity
{
    /// <summary>
    /// Application bootstrap (replaces Global.asax / Application_Start; no System.Web.HttpApplication / IIS dependency).
    ///
    /// Cloud-ready initialization model (GCP Cloud Run / GKE):
    ///  1. Configuration is NOT baked into instance start-up code. Secrets (e.g. the database connection string)
    ///     are resolved from GCP Secret Manager, so every scaled instance sees exactly the same, centrally managed values.
    ///     Set DB_CONNECTION_SECRET to a secret version resource name, e.g.
    ///     "projects/my-project/secrets/school-db-connection/versions/latest".
    ///  2. One-time, cluster-wide initialization (database migrations / seeding) is NOT executed by every
    ///     instance. It runs exactly once in a Cloud Run init container / job, started with the "--init" argument
    ///     (see <see cref="RunOneTimeInitialization"/> and <see cref="Program"/>).
    ///  3. Only stateless, in-process registration (EF interceptors) is performed per instance
    ///     (<see cref="Initialize"/>), because interceptors live in process memory and must exist in every instance.
    ///
    /// Pipeline concerns formerly handled in Application_Start are handled by the ASP.NET Core middleware pipeline in <see cref="Startup"/>:
    ///   - RouteConfig.RegisterRoutes      -> endpoint routing (MapControllerRoute)
    ///   - FilterConfig (HandleError)      -> UseExceptionHandler middleware
    ///   - BundleConfig                    -> UseStaticFiles middleware
    /// </summary>
    public static class MvcApplication
    {
        public const string ConnectionSecretEnvVar = "DB_CONNECTION_SECRET";
        public const string ConnectionStringEnvVar = "SCHOOL_DB_CONNECTION";

        private static int _registered;
        private static readonly Lazy<string> _connectionString = new Lazy<string>(LoadConnectionString, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Per-instance, stateless and idempotent registration of in-memory EF interceptors.
        /// Contains no shared/one-time work, so it is safe on any number of scaled instances.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 1) return;
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());
        }

        /// <summary>
        /// Connection string resolved from GCP Secret Manager (DB_CONNECTION_SECRET), then from the
        /// SCHOOL_DB_CONNECTION environment variable, falling back to the EF convention name.
        /// </summary>
        public static string GetConnectionString() => _connectionString.Value;

        /// <summary>
        /// One-time initialization intended to be executed by a Cloud Run init container / job (run to completion
        /// before the serving instances start). Applies EF migrations so scaled instances never race on schema changes.
        /// </summary>
        public static void RunOneTimeInitialization()
        {
            Initialize();
            var migrator = new DbMigrator(new Migrations.Configuration());
            migrator.Update();
            Console.WriteLine("{\"severity\":\"INFO\",\"message\":\"One-time database initialization completed\"}");
        }

        private static string LoadConnectionString()
        {
            var secretName = Environment.GetEnvironmentVariable(ConnectionSecretEnvVar);
            if (!string.IsNullOrWhiteSpace(secretName))
            {
                try
                {
                    var client = SecretManagerServiceClient.Create();
                    return client.AccessSecretVersion(secretName).Payload.Data.ToStringUtf8();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("{\"severity\":\"ERROR\",\"message\":\"Unable to read database connection from Secret Manager\",\"error\":\"" + ex.GetType().Name + "\"}");
                    throw;
                }
            }

            var fromEnv = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            return string.IsNullOrWhiteSpace(fromEnv) ? typeof(SchoolContext).FullName : fromEnv;
        }
    }
}
