namespace Alfred.Identity.Domain.Abstractions;

public interface IUserActivityLogger
{
    Task LogAsync(
        Guid userId,
        string action,
        string? description = null,
        CancellationToken cancellationToken = default);
}
