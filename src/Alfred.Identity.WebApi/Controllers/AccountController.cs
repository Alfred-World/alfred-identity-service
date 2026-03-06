using Alfred.Identity.Application.Auth.Commands.ChangePassword;
using Alfred.Identity.Application.Auth.Commands.RevokeSession;
using Alfred.Identity.Application.Auth.Commands.TwoFactor;
using Alfred.Identity.Application.Auth.Commands.UpdateProfile;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.WebApi.Contracts.Account;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("identity/account")]
[Produces("application/json")]
public class AccountController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IBackupCodeRepository _backupCodeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;

    public AccountController(
        IMediator mediator,
        ICurrentUser currentUser,
        IBackupCodeRepository backupCodeRepository,
        IUserRepository userRepository,
        ITokenRepository tokenRepository)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _backupCodeRepository = backupCodeRepository;
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user == null)
        {
            return NotFoundResponse("User not found");
        }

        return OkResponse(new ProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.Avatar,
            TwoFactorEnabled = user.TwoFactorEnabled,
            EmailConfirmed = user.EmailConfirmed
        });
    }

    /// <summary>
    /// Update current user's profile (full name, phone number, avatar)
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new UpdateProfileCommand(
            _currentUser.UserId.Value,
            request.FullName,
            request.PhoneNumber,
            request.Avatar
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        var updated = result.Value!;

        return OkResponse(new ProfileResponse
        {
            Id = updated.Id,
            FullName = updated.FullName,
            Email = updated.Email,
            UserName = updated.UserName,
            PhoneNumber = updated.PhoneNumber,
            Avatar = updated.Avatar,
            TwoFactorEnabled = false,
            EmailConfirmed = false
        });
    }

    /// <summary>
    /// Get all active sessions (devices) for the current user
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SessionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var tokens = await _tokenRepository.GetActiveSessionsByUserIdAsync(
            _currentUser.UserId.Value, cancellationToken);

        var currentUserAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var sessions = tokens.Select(t => new SessionDto
        {
            Id = t.Id,
            Device = t.Device ?? "Unknown Device",
            IpAddress = t.IpAddress,
            Location = t.Location,
            CreatedAt = t.CreationDate,
            ExpiresAt = t.ExpirationDate,
            IsCurrentSession = false // Frontend can mark current session by comparing with stored token
        });

        return OkResponse(sessions);
    }

    /// <summary>
    /// Revoke a specific session (device) — takes immediate effect via Redis
    /// </summary>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeSession([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new RevokeSessionCommand(_currentUser.UserId.Value, id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse("Session revoked successfully");
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new ChangePasswordCommand(_currentUser.UserId.Value, request.OldPassword, request.NewPassword);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse("Password changed successfully");
    }

    /// <summary>
    /// Initiate 2FA Setup (Enable)
    /// </summary>
    /// <returns>Secret Key and QR Code URI</returns>
    [HttpPost("2fa/enable")]
    [ProducesResponseType(typeof(ApiResponse<InitiateEnableTwoFactorResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InitiateEnableTwoFactor()
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new InitiateEnableTwoFactorCommand(_currentUser.UserId.Value, _currentUser.Email ?? "");
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Confirm 2FA Setup and get recovery codes
    /// </summary>
    [HttpPost("2fa/confirm")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmEnableTwoFactor([FromBody] ConfirmTwoFactorRequest request)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new ConfirmEnableTwoFactorCommand(_currentUser.UserId.Value, request.Code);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Disable 2FA
    /// </summary>
    [HttpPost("2fa/disable")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DisableTwoFactor()
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new DisableTwoFactorCommand(_currentUser.UserId.Value);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse("Two-factor authentication disabled successfully");
    }

    /// <summary>
    /// View recovery codes status (remaining count of unused codes)
    /// </summary>
    [HttpGet("2fa/recovery-codes")]
    [ProducesResponseType(typeof(ApiResponse<RecoveryCodeStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecoveryCodes(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var codes = await _backupCodeRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        var remaining = codes.Count(c => c.IsValid());

        return OkResponse(new RecoveryCodeStatusResponse(remaining, codes.Count));
    }

    /// <summary>
    /// Regenerate 10 new single-use recovery codes. All existing codes are invalidated immediately.
    /// </summary>
    [HttpPost("2fa/recovery-codes/regenerate")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegenerateRecoveryCodes(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return UnauthorizedResponse("User not identified");
        }

        var command = new RegenerateBackupCodesCommand(_currentUser.UserId.Value);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse(result.Value!);
    }
}
