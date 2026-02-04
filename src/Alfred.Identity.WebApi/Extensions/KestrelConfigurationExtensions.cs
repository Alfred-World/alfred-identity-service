using System.Security.Cryptography.X509Certificates;

using Alfred.Identity.WebApi.Configuration;

using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring Kestrel server with mTLS support
/// </summary>
public static class KestrelConfigurationExtensions
{
    /// <summary>
    /// Configure Kestrel with optional mTLS support
    /// </summary>
    public static WebApplicationBuilder ConfigureKestrelWithMtls(
        this WebApplicationBuilder builder,
        AppConfiguration appConfig,
        MtlsConfiguration mtlsConfig)
    {
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            if (mtlsConfig.Enabled)
            {
                ConfigureMtlsEndpoints(options, mtlsConfig);
            }
            else
            {
                ConfigureHttpOnlyEndpoint(options, appConfig);
            }
        });

        return builder;
    }

    private static void ConfigureMtlsEndpoints(
        Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options,
        MtlsConfiguration mtlsConfig)
    {
        // Load certificates
        var serverCert = mtlsConfig.LoadServerCertificate();
        var caCert = mtlsConfig.LoadCaCertificate();

        // HTTPS endpoint with client certificate requirement (mTLS)
        options.ListenAnyIP(mtlsConfig.HttpsPort, listenOptions =>
        {
            listenOptions.UseHttps(httpsOptions =>
            {
                httpsOptions.ServerCertificate = serverCert;
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                httpsOptions.ClientCertificateValidation = (certificate, chain, errors) =>
                    ValidateClientCertificate(certificate, chain, caCert);
            });
        });

        Console.WriteLine($"✅ mTLS enabled - HTTPS listening on port {mtlsConfig.HttpsPort}");

        // Optional HTTP endpoint for health checks
        if (mtlsConfig.AllowHttp)
        {
            options.ListenAnyIP(mtlsConfig.HttpPort);
            Console.WriteLine($"✅ HTTP (health checks) listening on port {mtlsConfig.HttpPort}");
        }
    }

    private static void ConfigureHttpOnlyEndpoint(
        Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options,
        AppConfiguration appConfig)
    {
        options.ListenAnyIP(appConfig.AppPort);
        Console.WriteLine($"ℹ️ mTLS disabled - HTTP listening on port {appConfig.AppPort}");
    }

    private static bool ValidateClientCertificate(
        X509Certificate2 certificate,
        X509Chain? chain,
        X509Certificate2 caCert)
    {
        // Validate that the client certificate is signed by our CA
        if (chain == null)
        {
            chain = new X509Chain();
        }

        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        chain.ChainPolicy.ExtraStore.Add(caCert);

        var isValid = chain.Build(certificate);
        if (!isValid)
        {
            return false;
        }

        // Verify the certificate chain ends with our CA
        var chainContainsCa = chain.ChainElements
            .Any(element => element.Certificate.Thumbprint == caCert.Thumbprint);

        return chainContainsCa;
    }
}
