using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Create;

public record CreateApplicationCommand(
    string ClientId,
    string ClientSecret,
    string DisplayName,
    string RedirectUris, // Space delimited
    string PostLogoutRedirectUris, // Space delimited
    string Permissions, // Space delimited scopes usually or permissions
    string Type = "public" // public or confidential
) : IRequest<long>;
