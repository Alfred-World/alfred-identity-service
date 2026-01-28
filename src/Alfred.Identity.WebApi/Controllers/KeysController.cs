using Alfred.Identity.Application.Auth.Commands.Rotation;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Authorize(Roles = "Admin,Owner")] // Strict auth
[Route("api/v1/keys")]
public class KeysController : BaseApiController
{
    private readonly IMediator _mediator;

    public KeysController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Manually rotate authentication signing keys
    /// </summary>
    /// <remarks>
    /// Forces the generation of a new signing key and adds it to the key ring.
    /// Old keys remain valid for verification until they expire.
    /// Requires Admin or Owner role.
    /// </remarks>
    [HttpPost("rotate")]
    [ProducesResponseType(typeof(RotateSigningKeyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RotateSigningKeyResult>> RotateKeys()
    {
        var result = await _mediator.Send(new RotateSigningKeyCommand());
        return Ok(result);
    }
}
