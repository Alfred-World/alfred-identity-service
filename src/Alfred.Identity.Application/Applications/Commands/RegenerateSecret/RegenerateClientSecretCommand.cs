using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.RegenerateSecret;

public record RegenerateClientSecretCommand(Guid Id) : IRequest<string?>;
