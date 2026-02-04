using System.Text.Json;
using Alfred.Identity.Domain.Abstractions.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Alfred.Identity.Infrastructure.Services;

public class RedisEmailSender : IEmailSender
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisEmailSender> _logger;
    private const string QueueKey = "alfred:notifications:email";

    public RedisEmailSender(IConnectionMultiplexer redis, ILogger<RedisEmailSender> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to, 
        string subject, 
        string htmlBody, 
        string? templateCode = null, 
        object? templateParams = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var payload = new
            {
                to,
                subject,
                html = htmlBody,
                templateCode,
                @params = templateParams
            };

            var json = JsonSerializer.Serialize(payload);
            
            // Push to the left of the list (Producer)
            await db.ListLeftPushAsync(QueueKey, json);
            
            _logger.LogInformation("Email job queued for {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email for {To}", to);
            throw;
        }
    }
}
