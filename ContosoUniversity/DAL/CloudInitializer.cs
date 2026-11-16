using System;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.Migrations;
using ContosoUniversity.Migrations;
using ContosoUniversity.Security;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Replaces the instance-specific Global.asax Application_Start initialization.
    ///
    /// - Configuration/secrets are loaded from GCP Secret Manager (centralized, identical for all instances).
    /// - One-time shared initialization (database migrations) is executed by a single coordinated
    ///   Cloud Run init step / job started with APP_RUN_MODE=init, never by each scaled instance.
    /// - Serving instances only perform idempotent, stateless per-process registration.
    ///
    /// Environment variables:
    ///   APP_RUN_MODE                 - "init" to run one-time initialization and exit
    ///   GCP_PROJECT_ID               - GCP project hosting the secrets
    ///   DB_CONNECTION_STRING_SECRET  - Secret Manager secret name holding the DB connection string
    ///   DB_CONNECTION_STRING_VERSION - secret version (default: latest)
    ///   DB_CONNECTION_STRING         - resulting connection string (may also be injected by Cloud Run secret mapping)
    /// </summary>
    public static class CloudInitializer
    {
        private static readonly object Sync = new object();
        private static bool _interceptorsRegistered;

        public static bool IsInitMode()
        {
            return string.Equals(Environment.GetEnvironmentVariable("APP_RUN_MODE"), "init",
                StringComparison.OrdinalIgnoreCase);
        }

        public static void LoadSecretsIntoEnvironment()
        {
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
            var secret = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_SECRET");
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(secret) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")))
            {
                return;
            }

            var version = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_VERSION");
            if (string.IsNullOrWhiteSpace(version)) version = "latest";

            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING",
                GcpSecretManagerPolicyProvider.AccessSecret(projectId, secret, version));
        }

        public static void RegisterInterceptors()
        {
            lock (Sync)
            {
                if (_interceptorsRegistered) return;
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
                _interceptorsRegistered = true;
            }
        }

        public static void RunOneTimeInitialization()
        {
            RegisterInterceptors();
            var migrator = new DbMigrator(new Configuration());
            migrator.Update();
        }
    }
}
