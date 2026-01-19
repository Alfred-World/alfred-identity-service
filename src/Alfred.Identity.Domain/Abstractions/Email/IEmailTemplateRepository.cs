using Alfred.Identity.Domain.EmailTemplates;

namespace Alfred.Identity.Domain.Abstractions.Email;

/// <summary>
/// Repository interface for EmailTemplate entity
/// </summary>
public interface IEmailTemplateRepository : IRepository<EmailTemplate, Guid>
{
    Task<EmailTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<EmailTemplate?> GetByCategoryAsync(EmailTemplateCategory category,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
