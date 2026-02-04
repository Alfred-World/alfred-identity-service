using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;

using Microsoft.AspNetCore.Http;

namespace Alfred.Identity.Infrastructure.Common.Identity;

public class UserActivityLogger : IUserActivityLogger
{
    private readonly IDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserActivityLogger(IDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(Guid userId, string action, string? description = null, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

        var log = UserActivityLog.Create(userId, action, description, ipAddress, userAgent);
        
        await _dbContext.Set<UserActivityLog>().AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

