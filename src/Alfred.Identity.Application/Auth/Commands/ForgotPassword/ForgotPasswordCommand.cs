using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ForgotPassword;

/// <param name="Email">The user's email address.</param>
/// <param name="ResetBaseUrl">Base URL of the identity web app used to build the reset link (e.g. https://identity.app).</param>
public record ForgotPasswordCommand(string Email, string ResetBaseUrl) : IRequest<Result<bool>>;
