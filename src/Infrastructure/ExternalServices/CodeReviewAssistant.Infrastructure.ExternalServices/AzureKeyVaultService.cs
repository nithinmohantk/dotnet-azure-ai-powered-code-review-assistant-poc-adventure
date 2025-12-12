using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeReviewAssistant.Infrastructure.ExternalServices
{
    public interface IAzureKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
        Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
        Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
        Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
    }

    public class AzureKeyVaultService : IAzureKeyVaultService
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<AzureKeyVaultService> _logger;

        public AzureKeyVaultService(IConfiguration configuration, ILogger<AzureKeyVaultService> logger)
        {
            var keyVaultUri = configuration["Azure:KeyVault:Uri"];
            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new ArgumentException("Azure Key Vault URI is not configured");
            }

            _secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving secret {SecretName} from Azure Key Vault", secretName);
                
                var response = await _secretClient.GetSecretAsync(secretName, cancellationToken);
                
                _logger.LogDebug("Successfully retrieved secret {SecretName}", secretName);
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Azure Key Vault", secretName);
                throw;
            }
        }

        public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Setting secret {SecretName} in Azure Key Vault", secretName);
                
                await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
                
                _logger.LogDebug("Successfully set secret {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret {SecretName} in Azure Key Vault", secretName);
                throw;
            }
        }

        public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Deleting secret {SecretName} from Azure Key Vault", secretName);
                
                await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
                
                _logger.LogDebug("Successfully deleted secret {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete secret {SecretName} from Azure Key Vault", secretName);
                throw;
            }
        }

        public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
                
                await foreach (var secretProperties in response)
                {
                    if (secretProperties.Name.Equals(secretName, StringComparison.OrdinalIgnoreCase))
                    {
                        return !secretProperties.Attributes.Enabled.HasValue || 
                               secretProperties.Attributes.Enabled.Value;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if secret {SecretName} exists in Azure Key Vault", secretName);
                throw;
            }
        }
    }
}
