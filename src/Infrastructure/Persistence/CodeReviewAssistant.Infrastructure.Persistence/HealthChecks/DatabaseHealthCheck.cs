// src/Infrastructure/Persistence/CodeReviewAssistant.Infrastructure.Persistence/HealthChecks/DatabaseHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace CodeReviewAssistant.Infrastructure.Persistence.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public DatabaseHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                
                return HealthCheckResult.Healthy("Database connection is OK");
            }
            catch (DbException ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }
}