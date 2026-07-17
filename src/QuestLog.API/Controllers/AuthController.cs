using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Auth.Commands.ForgotPassword;
using QuestLog.Application.Auth.Commands.Login;
using QuestLog.Application.Auth.Commands.Register;
using QuestLog.Application.Auth.Commands.ResetPassword;

namespace QuestLog.API.Controllers;

/// <summary>
/// Public endpoints for authentication. These routes do NOT require a JWT token.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Creates a new user account and returns a JWT token.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Authenticates with email + password and returns a JWT token.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Generates a password reset token and logs it to the console (mock email).
    /// Always returns 200 regardless of whether the email exists, to prevent enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "If that email is registered, a reset token has been sent." });
    }

    /// <summary>Validates the reset token and updates the user's password.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "Password reset successfully." });
    }
}
