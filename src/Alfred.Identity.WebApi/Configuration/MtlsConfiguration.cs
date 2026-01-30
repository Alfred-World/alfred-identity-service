using System.Security.Cryptography.X509Certificates;

namespace Alfred.Identity.WebApi.Configuration;

/// <summary>
/// Configuration for mTLS (mutual TLS) between services.
/// Handles server certificate and client certificate validation.
/// </summary>
public class MtlsConfiguration
{
    /// <summary>
    /// Whether mTLS is enabled for this service
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Path to the server certificate (PFX file)
    /// </summary>
    public string? ServerCertPath { get; }

    /// <summary>
    /// Password for the server certificate PFX file
    /// </summary>
    public string? ServerCertPassword { get; }

    /// <summary>
    /// Path to the CA certificate for validating client certificates
    /// </summary>
    public string? CaCertPath { get; }

    /// <summary>
    /// HTTPS port for mTLS communication (separate from HTTP port)
    /// </summary>
    public int HttpsPort { get; }

    /// <summary>
    /// Whether to also listen on HTTP (for health checks, etc.)
    /// </summary>
    public bool AllowHttp { get; }

    /// <summary>
    /// HTTP port (if AllowHttp is true)
    /// </summary>
    public int HttpPort { get; }

    public MtlsConfiguration()
    {
        Enabled = GetBool("MTLS_ENABLED", false);
        ServerCertPath = GetOptional("MTLS_SERVER_CERT_PATH");
        ServerCertPassword = GetOptional("MTLS_SERVER_CERT_PASSWORD") ?? "";
        CaCertPath = GetOptional("MTLS_CA_CERT_PATH");
        HttpsPort = GetInt("MTLS_HTTPS_PORT", 8101);
        AllowHttp = GetBool("MTLS_ALLOW_HTTP", true);
        HttpPort = GetInt("MTLS_HTTP_PORT", 8100);

        if (Enabled)
        {
            ValidateConfiguration();
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(ServerCertPath))
        {
            throw new InvalidOperationException(
                "MTLS_SERVER_CERT_PATH is required when MTLS_ENABLED=true");
        }

        if (!File.Exists(ServerCertPath))
        {
            throw new InvalidOperationException(
                $"Server certificate not found at: {ServerCertPath}");
        }

        if (string.IsNullOrWhiteSpace(CaCertPath))
        {
            throw new InvalidOperationException(
                "MTLS_CA_CERT_PATH is required when MTLS_ENABLED=true");
        }

        if (!File.Exists(CaCertPath))
        {
            throw new InvalidOperationException(
                $"CA certificate not found at: {CaCertPath}");
        }
    }

    /// <summary>
    /// Load the server certificate from the PFX file
    /// </summary>
    public X509Certificate2 LoadServerCertificate()
    {
        if (string.IsNullOrWhiteSpace(ServerCertPath))
        {
            throw new InvalidOperationException("Server certificate path is not configured");
        }

        return X509CertificateLoader.LoadPkcs12FromFile(
            ServerCertPath,
            ServerCertPassword,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
    }

    /// <summary>
    /// Load the CA certificate for validating client certificates
    /// </summary>
    public X509Certificate2 LoadCaCertificate()
    {
        if (string.IsNullOrWhiteSpace(CaCertPath))
        {
            throw new InvalidOperationException("CA certificate path is not configured");
        }

        return X509CertificateLoader.LoadCertificateFromFile(CaCertPath);
    }

    private static string GetOptional(string key)
    {
        return Environment.GetEnvironmentVariable(key) ?? string.Empty;
    }

    private static int GetInt(string key, int defaultValue)
    {
        var value = GetOptional(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : int.Parse(value);
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        var value = GetOptional(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }
}
