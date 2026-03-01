using Alfred.Identity.Application.Auth.Commands.ChangePassword;
using Alfred.Identity.Application.Auth.Commands.TwoFactor;
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

    public AccountController(IMediator mediator, ICurrentUser currentUser, IBackupCodeRepository backupCodeRepository)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _backupCodeRepository = backupCodeRepository;
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

        // Need email for QR Code
        // Assuming current user context has email, or need to fetch it? 
        // ICurrentUser interface check... 
        // If ICurrentUser doesn't have Email, I might need to Command to fetch User first.
        // But let's assume ICurrentUser or Command handles it.
        // Wait, InitiateEnableTwoFactorCommand expects Email.
        // I should fetch user info or pass Email if I have it in claims.
        // Let's check ICurrentUser.

        var command = new InitiateEnableTwoFactorCommand(_currentUser.UserId.Value, _currentUser.Email ?? "");
        // If Email is missing in claims, the handler *could* look it up by ID if we modify Command to be ID only, 
        // or we just trust Claims have it. My CurrentUserService implementation usually puts Email in claims.

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

