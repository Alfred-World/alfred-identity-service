using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Applications.Queries.Get;

public record GetApplicationQuery(long Id) : IRequest<ApplicationDto?>;

// Minimal DTO
public record ApplicationDto(
    long Id,
    string ClientId,
    string DisplayName,
    string RedirectUris,
    string PostLogoutRedirectUris,
    string Permissions,
    string Type
);

public class GetApplicationQueryHandler : IRequestHandler<GetApplicationQuery, ApplicationDto?>
{
    private readonly IApplicationRepository _applicationRepository;

    public GetApplicationQueryHandler(IApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public async Task<ApplicationDto?> Handle(GetApplicationQuery request, CancellationToken cancellationToken)
    {
        var app = await _applicationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (app == null)
        {
            return null;
        }

        return new ApplicationDto(
            app.Id,
            app.ClientId,
            app.DisplayName ?? "",
            app.RedirectUris ?? "",
            app.PostLogoutRedirectUris ?? "",
            app.Permissions ?? "",
            app.ClientType ?? "public"
        );
    }
}
