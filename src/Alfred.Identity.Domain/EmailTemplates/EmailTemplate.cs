using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;

using DomainException = Alfred.Identity.Domain.Common.Exceptions.DomainException;

namespace Alfred.Identity.Domain.EmailTemplates;

public sealed class EmailTemplate : BaseEntity<Guid>,
    IHasCreationTime, IHasCreator,
    IHasModificationTime, IHasModifier,
    IHasDeletionTime, IHasDeleter,
    IHasDomainEvents
{
    // Constants for validation rules to avoid magic numbers
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 100;
    public const int MaxSubjectLength = 256;
    public const int MaxDescriptionLength = 500;

    private readonly List<IDomainEvent> _domainEvents = new();

    #region Properties

    /// <summary>
    /// Template code (unique identifier for template)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Email subject with placeholders
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// HTML body with placeholders
    /// </summary>
    public string HtmlBody { get; private set; } = string.Empty;

    /// <summary>
    /// Plain text body (optional fallback)
    /// </summary>
    public string? PlainTextBody { get; private set; }

    /// <summary>
    /// Description of template usage
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Available placeholders for this template (JSON array)
    /// </summary>
    public string? AvailablePlaceholders { get; private set; }

    /// <summary>
    /// Is this template active?
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Is this a system template (cannot be deleted/updated critical info)
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Category of template
    /// </summary>
    public EmailTemplateCategory Category { get; private set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public long? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long? DeletedById { get; set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    #endregion

    #region Constructors

    private EmailTemplate()
    {
    }

    private EmailTemplate(
        string code,
        string name,
        string subject,
        string htmlBody,
        EmailTemplateCategory category,
        string? description,
        string? plainTextBody,
        string? availablePlaceholders,
        bool isSystem,
        long? createdById)
    {
        // Call individual setter methods
        SetCode(code);
        SetName(name);
        SetSubject(subject);
        SetHtmlBody(htmlBody);

        // Optional fields
        if (!string.IsNullOrWhiteSpace(description))
        {
            SetDescription(description);
        }

        PlainTextBody = plainTextBody;
        AvailablePlaceholders = availablePlaceholders;
        Category = category;
        IsSystem = isSystem;
        IsActive = true;

        CreatedAt = DateTime.UtcNow;
        CreatedById = createdById;
        IsDeleted = false;

        // You can add domain event if needed
        // AddDomainEvent(new EmailTemplateCreatedEvent(Id, Code)); 
    }

    #endregion

    #region Factory Methods

    public static EmailTemplate Create(
        string code,
        string name,
        string subject,
        string htmlBody,
        EmailTemplateCategory category,
        string? description = null,
        string? plainTextBody = null,
        string? availablePlaceholders = null,
        bool isSystem = false,
        long? createdById = null)
    {
        return new EmailTemplate(
            code, name, subject, htmlBody, category,
            description, plainTextBody, availablePlaceholders, isSystem, createdById);
    }

    #endregion

    #region Business Methods

    public void Update(
        string name,
        string subject,
        string htmlBody,
        EmailTemplateCategory category,
        string? description = null,
        string? plainTextBody = null,
        string? availablePlaceholders = null,
        long? updatedById = null)
    {
        if (IsSystem)
        {
            throw new DomainException("System templates cannot be updated");
        }

        SetName(name);
        SetSubject(subject);
        SetHtmlBody(htmlBody);

        if (description != null)
        {
            SetDescription(description);
        }

        PlainTextBody = plainTextBody;
        AvailablePlaceholders = availablePlaceholders;
        Category = category;

        UpdatedById = updatedById;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate(long? updatedById = null)
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }

    public void Deactivate(long? updatedById = null)
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }

    public void Delete(long deletedById)
    {
        if (IsSystem)
        {
            throw new DomainException("System templates cannot be deleted");
        }

        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedById = deletedById;

        // AddDomainEvent(new EmailTemplateDeletedEvent(Id));
    }

    public void Restore(long restoredById)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedById = null;
        UpdatedById = restoredById;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Internal Setters & Validation

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Template code cannot be empty");
        }

        code = code.Trim().ToUpperInvariant();

        if (code.Length > MaxCodeLength)
        {
            throw new DomainException($"Template code cannot exceed {MaxCodeLength} characters");
        }

        Code = code;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Template name cannot be empty");
        }

        name = name.Trim();

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Template name cannot exceed {MaxNameLength} characters");
        }

        Name = name;
    }

    private void SetSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("Email subject cannot be empty");
        }

        subject = subject.Trim();

        if (subject.Length > MaxSubjectLength)
        {
            throw new DomainException($"Email subject cannot exceed {MaxSubjectLength} characters");
        }

        Subject = subject;
    }

    private void SetHtmlBody(string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new DomainException("Email body cannot be empty");
        }

        // Don't trim body as HTML may need whitespace
        HtmlBody = htmlBody;
    }

    private void SetDescription(string description)
    {
        description = description?.Trim() ?? string.Empty;

        if (description.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Description cannot exceed {MaxDescriptionLength} characters");
        }

        Description = description;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    #endregion
}

/// <summary>
/// Email template category
/// </summary>
public enum EmailTemplateCategory
{
    Authentication = 1,
    Notification = 2,
    Marketing = 3,
    System = 4
}
