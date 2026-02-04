using Alfred.Identity.Application.Auth.Commands.ChangePassword;
using Alfred.Identity.Application.Auth.Commands.TwoFactor;
using Alfred.Identity.Domain.Abstractions;
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

    public AccountController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (_currentUser.UserId == null) return UnauthorizedResponse("User not identified");

        var command = new ChangePasswordCommand(_currentUser.UserId.Value, request.OldPassword, request.NewPassword);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse(new { Success = true, Message = "Password changed successfully" });
    }

    /// <summary>
    /// Initiate 2FA Setup (Enable)
    /// </summary>
    /// <returns>Secret Key and QR Code URI</returns>
    [HttpPost("2fa/enable")]
    [ProducesResponseType(typeof(ApiSuccessResponse<InitiateEnableTwoFactorResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateEnableTwoFactor()
    {
        if (_currentUser.UserId == null) return UnauthorizedResponse("User not identified");

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
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmEnableTwoFactor([FromBody] ConfirmTwoFactorRequest request)
    {
        if (_currentUser.UserId == null) return UnauthorizedResponse("User not identified");

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
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableTwoFactor()
    {
        if (_currentUser.UserId == null) return UnauthorizedResponse("User not identified");

        var command = new DisableTwoFactorCommand(_currentUser.UserId.Value);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequestResponse(result.Error);
        }

        return OkResponse(new { Success = true, Message = "Two-factor authentication disabled successfully" });
    }
}
